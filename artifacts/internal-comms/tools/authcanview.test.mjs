// Regression test for authCanView() in artifacts/internal-comms/index.html.
//
// The function used to hardcode `return !!AUTH_USER` for seven pages, so any signed-in user
// reached them whatever the permission matrix said. Nothing caught that, because nothing tests
// this function at all -- the Playwright harness drives the UI as a super admin, who
// short-circuits at the top and can therefore never observe the bypass.
//
// Rather than mock the whole 15k-line page, this lifts the four functions and two lookup
// tables that decide visibility out of the HTML and evaluates them against constructed users.
// Extraction is by exact source slice and fails loudly if a function is renamed or removed,
// so the test cannot silently stop testing anything.

import { readFileSync } from 'node:fs';

const HTML = process.env.ICBANK_HTML || new URL('../index.html', import.meta.url).pathname;
const src = readFileSync(HTML, 'utf8');

function sliceDecl(name, kind) {
  const needle = kind === 'var' ? `var ${name} = {` : `function ${name}(`;
  const start = src.indexOf(needle);
  if (start === -1) throw new Error(`could not find ${kind} ${name} -- extraction is stale`);
  let i = src.indexOf('{', start), depth = 0;
  for (; i < src.length; i++) {
    if (src[i] === '{') depth++;
    else if (src[i] === '}') { depth--; if (depth === 0) break; }
  }
  if (depth !== 0) throw new Error(`unbalanced braces reading ${name}`);
  const end = kind === 'var' ? src.indexOf(';', i) + 1 : i + 1;
  return src.slice(start, end);
}

const harness = [
  sliceDecl('PAGE_PERM_MAP', 'var'),
  sliceDecl('LIBRARY_TAB_PAGE', 'var'),
  sliceDecl('authCanView', 'fn'),
  sliceDecl('librariesTabAllowed', 'fn'),
  sliceDecl('librariesFirstAllowedTab', 'fn'),
].join('\n');

const make = new Function(`
  var AUTH_USER = null;
  ${harness}
  return function (user, page) { AUTH_USER = user; return authCanView(page); };
`)();

const failures = [];
function check(label, actual, expected) {
  const ok = actual === expected;
  console.log(`  ${ok ? 'PASS' : 'FAIL'}  ${label}${ok ? '' : `  -- expected ${expected}, got ${actual}`}`);
  if (!ok) failures.push(label);
}

const user = (perms, extra = {}) => ({
  role: 'viewer', isSuperAdmin: false, permissions: perms, ...extra,
});

// The seven pages that were hardcoded. A user with an empty matrix must see none of them.
const BYPASSED = ['shorfah', 'media_monitoring', 'libraries', 'smart_assistant',
                  'settings', 'gac_library', 'prompts_lib'];
const empty = user({});
console.log('=== the seven formerly-bypassed pages, user with no grants ===');
for (const page of BYPASSED) check(`${page} is hidden`, make(empty, page), false);

console.log('\n=== the same pages open once the matrix grants them ===');
check('shorfah with shorfah:view', make(user({ shorfah: ['view'] }), 'shorfah'), true);
check('media_monitoring with its grant',
      make(user({ media_monitoring: ['view'] }), 'media_monitoring'), true);
check('settings with settings:view', make(user({ settings: ['view'] }), 'settings'), true);
check('smart_assistant with its grant',
      make(user({ smart_assistant: ['view'] }), 'smart_assistant'), true);
check('gac_library follows design_studio',
      make(user({ design_studio: ['view'] }), 'gac_library'), true);
check('prompts_lib follows smart_assistant',
      make(user({ smart_assistant: ['view'] }), 'prompts_lib'), true);

console.log('\n=== a non-view verb must not grant visibility ===');
check('shorfah with only edit stays hidden', make(user({ shorfah: ['edit'] }), 'shorfah'), false);
check('settings with only export stays hidden',
      make(user({ settings: ['export'] }), 'settings'), false);

console.log('\n=== libraries is a shell: visible when any pane is ===');
check('hidden when no pane is allowed', make(user({}), 'libraries'), false);
check('visible via the gac pane (design_studio)',
      make(user({ design_studio: ['view'] }), 'libraries'), true);
check('visible via the places pane (weekend)',
      make(user({ weekend: ['view'] }), 'libraries'), true);
check('an unrelated grant does not open it',
      make(user({ shorfah: ['view'] }), 'libraries'), false);

console.log('\n=== role name alone no longer grants access ===');
check('admin without design_studio cannot see designs',
      make(user({}, { role: 'admin' }), 'designs'), false);
check('admin without admin_panel cannot see the admin panel',
      make(user({}, { role: 'admin' }), 'admin_panel'), false);
check('admin with the grant can',
      make(user({ admin_panel: ['view'] }, { role: 'admin' }), 'admin_panel'), true);

console.log('\n=== unchanged behaviour ===');
check('super admin sees everything',
      make(user({}, { isSuperAdmin: true }), 'shorfah'), true);
check('signed-out sees nothing', make(null, 'home'), false);
check('home stays open to any signed-in user', make(empty, 'home'), true);

console.log();
if (failures.length) {
  console.log(`  ${failures.length} check(s) failed`);
  process.exit(1);
}
console.log('  all authCanView checks passed');
