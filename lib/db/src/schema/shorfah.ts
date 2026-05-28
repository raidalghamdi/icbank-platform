import {
  pgTable,
  serial,
  text,
  boolean,
  integer,
  timestamp,
} from "drizzle-orm/pg-core";

export const shorfahIssuesTable = pgTable("shorfah_issues", {
  id: serial("id").primaryKey(),
  issueNo: integer("issue_no").notNull().unique(),
  titleAr: text("title_ar").notNull(),
  subtitleAr: text("subtitle_ar"),
  month: integer("month").notNull(),
  year: integer("year").notNull(),
  coverImageUrl: text("cover_image_url"),
  editorLetter: text("editor_letter"),
  contributionsOpenAt: timestamp("contributions_open_at", { withTimezone: true }),
  contributionsCloseAt: timestamp("contributions_close_at", { withTimezone: true }),
  status: text("status").notNull().default("collecting"),
  publishedPdfUrl: text("published_pdf_url"),
  publishedAt: timestamp("published_at", { withTimezone: true }),
  createdBy: integer("created_by"),
  createdAt: timestamp("created_at", { withTimezone: true }).defaultNow(),
  updatedAt: timestamp("updated_at", { withTimezone: true }).defaultNow(),
});

export const shorfahSectionsTable = pgTable("shorfah_sections", {
  id: serial("id").primaryKey(),
  issueId: integer("issue_id").notNull(),
  parentSectionId: integer("parent_section_id"),
  sectionType: text("section_type").notNull(),
  titleAr: text("title_ar").notNull(),
  descriptionAr: text("description_ar"),
  displayOrder: integer("display_order").notNull().default(0),
  ownerUserId: integer("owner_user_id"),
  ownerRole: text("owner_role"),
  includeInPdf: boolean("include_in_pdf").notNull().default(true),
  autoGenerate: boolean("auto_generate").default(false),
  generationPrompt: text("generation_prompt"),
  workflowStatus: text("workflow_status").notNull().default("pending_contribution"),
  contentMd: text("content_md"),
  contentHtml: text("content_html"),
  contributedBy: integer("contributed_by"),
  contributedAt: timestamp("contributed_at", { withTimezone: true }),
  reviewedBy: integer("reviewed_by"),
  reviewedAt: timestamp("reviewed_at", { withTimezone: true }),
  reviewNotes: text("review_notes"),
  approvedBy: integer("approved_by"),
  approvedAt: timestamp("approved_at", { withTimezone: true }),
  rejectionReason: text("rejection_reason"),
  // SLA fields (Task 3)
  slaDays: integer("sla_days").default(7),
  slaStartsAt: timestamp("sla_starts_at", { withTimezone: true }),
  slaDeadline: timestamp("sla_deadline", { withTimezone: true }),
  createdAt: timestamp("created_at", { withTimezone: true }).defaultNow(),
  updatedAt: timestamp("updated_at", { withTimezone: true }).defaultNow(),
});

export const shorfahSectionPermissionsTable = pgTable("shorfah_section_permissions", {
  id: serial("id").primaryKey(),
  sectionId: integer("section_id").notNull(),
  userId: integer("user_id"),
  roleName: text("role_name"),
  permission: text("permission").notNull(),
  createdAt: timestamp("created_at", { withTimezone: true }).defaultNow(),
});

export const shorfahSectionMediaTable = pgTable("shorfah_section_media", {
  id: serial("id").primaryKey(),
  sectionId: integer("section_id").notNull(),
  mediaUrl: text("media_url").notNull(),
  mediaType: text("media_type").notNull(),
  captionAr: text("caption_ar"),
  displayOrder: integer("display_order").default(0),
  createdAt: timestamp("created_at", { withTimezone: true }).defaultNow(),
});

export const shorfahWorkflowLogTable = pgTable("shorfah_workflow_log", {
  id: serial("id").primaryKey(),
  sectionId: integer("section_id").notNull(),
  actorUserId: integer("actor_user_id"),
  action: text("action").notNull(),
  fromStatus: text("from_status"),
  toStatus: text("to_status"),
  notes: text("notes"),
  createdAt: timestamp("created_at", { withTimezone: true }).defaultNow(),
});

// Task 4: New tables
export const shorfahAssignmentsTable = pgTable("shorfah_assignments", {
  id: serial("id").primaryKey(),
  sectionId: integer("section_id").notNull(),
  userId: integer("user_id").notNull(),
  role: text("role").default("contributor"),
  createdAt: timestamp("created_at", { withTimezone: true }).defaultNow(),
});

export const shorfahRemindersTable = pgTable("shorfah_reminders", {
  id: serial("id").primaryKey(),
  sectionId: integer("section_id").notNull(),
  assignmentId: integer("assignment_id"),
  recipientUserId: integer("recipient_user_id").notNull(),
  channel: text("channel").notNull(),
  reminderType: text("reminder_type").notNull(),
  sentAt: timestamp("sent_at", { withTimezone: true }).defaultNow(),
  status: text("status").default("sent"),
  message: text("message"),
});

// Round 3 Task 1: SLA defaults per section type
export const shorfahSectionSlaDefaultsTable = pgTable("shorfah_section_sla_defaults", {
  sectionType: text("section_type").primaryKey(),
  slaDays: integer("sla_days").notNull().default(7),
  updatedAt: timestamp("updated_at", { withTimezone: true }).defaultNow(),
  updatedBy: integer("updated_by"),
});

export const shorfahNotificationsTable = pgTable("shorfah_notifications", {
  id: serial("id").primaryKey(),
  userId: integer("user_id").notNull(),
  issueId: integer("issue_id"),
  sectionId: integer("section_id"),
  type: text("type").notNull(),
  title: text("title").notNull(),
  body: text("body"),
  url: text("url"),
  isRead: boolean("is_read").default(false),
  createdAt: timestamp("created_at", { withTimezone: true }).defaultNow(),
});
