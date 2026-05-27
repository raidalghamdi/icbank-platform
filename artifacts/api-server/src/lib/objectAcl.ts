/**
 * ACL layer for object storage.
 *
 * Originally the ACL policy was persisted as GCS custom metadata
 * ("custom:aclPolicy" -> JSON). Supabase Storage does not expose per-object
 * custom metadata in the same way, so we persist policies in a small Postgres
 * table `object_acl(key text primary key, policy jsonb)` accessed via the
 * Supabase service-role client.
 *
 * The exported function signatures are identical to the previous module so
 * route code does not need to change.
 */
import type { StorageObject } from "./objectStorage";
import { getSupabase } from "./objectStorage";

const ACL_TABLE = "object_acl";

// Access-group taxonomy (extension point — none implemented out of the box).
export enum ObjectAccessGroupType {}

export interface ObjectAccessGroup {
  type: ObjectAccessGroupType;
  id: string;
}

export enum ObjectPermission {
  READ = "read",
  WRITE = "write",
}

export interface ObjectAclRule {
  group: ObjectAccessGroup;
  permission: ObjectPermission;
}

export interface ObjectAclPolicy {
  owner: string;
  visibility: "public" | "private";
  aclRules?: Array<ObjectAclRule>;
}

function isPermissionAllowed(
  requested: ObjectPermission,
  granted: ObjectPermission
): boolean {
  if (requested === ObjectPermission.READ) {
    return [ObjectPermission.READ, ObjectPermission.WRITE].includes(granted);
  }
  return granted === ObjectPermission.WRITE;
}

abstract class BaseObjectAccessGroup implements ObjectAccessGroup {
  constructor(
    public readonly type: ObjectAccessGroupType,
    public readonly id: string
  ) {}

  public abstract hasMember(userId: string): Promise<boolean>;
}

function createObjectAccessGroup(group: ObjectAccessGroup): BaseObjectAccessGroup {
  switch (group.type) {
    default:
      throw new Error(`Unknown access group type: ${group.type}`);
  }
}

export async function setObjectAclPolicy(
  objectFile: StorageObject,
  aclPolicy: ObjectAclPolicy
): Promise<void> {
  const supabase = getSupabase();
  const { error } = await supabase
    .from(ACL_TABLE)
    .upsert({ key: objectFile.key, policy: aclPolicy }, { onConflict: "key" });
  if (error) {
    throw new Error(`Failed to persist ACL policy: ${error.message}`);
  }
}

export async function getObjectAclPolicy(
  objectFile: StorageObject
): Promise<ObjectAclPolicy | null> {
  const supabase = getSupabase();
  const { data, error } = await supabase
    .from(ACL_TABLE)
    .select("policy")
    .eq("key", objectFile.key)
    .maybeSingle();
  if (error) {
    // Table may not exist yet in fresh environments — treat as "no policy".
    return null;
  }
  return (data?.policy as ObjectAclPolicy | null) ?? null;
}

export async function canAccessObject({
  userId,
  objectFile,
  requestedPermission,
}: {
  userId?: string;
  objectFile: StorageObject;
  requestedPermission: ObjectPermission;
}): Promise<boolean> {
  const aclPolicy = await getObjectAclPolicy(objectFile);
  if (!aclPolicy) {
    return false;
  }

  if (
    aclPolicy.visibility === "public" &&
    requestedPermission === ObjectPermission.READ
  ) {
    return true;
  }

  if (!userId) {
    return false;
  }

  if (aclPolicy.owner === userId) {
    return true;
  }

  for (const rule of aclPolicy.aclRules || []) {
    const accessGroup = createObjectAccessGroup(rule.group);
    if (
      (await accessGroup.hasMember(userId)) &&
      isPermissionAllowed(requestedPermission, rule.permission)
    ) {
      return true;
    }
  }

  return false;
}
