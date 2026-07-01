const { webAppRoleId } = require("./app-config");
const SUPER_ADMIN_ROLE_ID = 5;
const superAdminOnlyScreenKeys = new Set(["admin-billing"]);

function isWebAppUser(user) {
  if (!user) return false;

  const roleId = Number(user.roleId ?? user.RoleId ?? user.roleID ?? user.role_id);
  if (roleId === webAppRoleId) {
    return true;
  }
  return false;
}

function isSuperAdmin(user) {
  if (!user) return false;

  const roleId = Number(user.roleId ?? user.RoleId ?? user.roleID ?? user.role_id);
  return roleId === SUPER_ADMIN_ROLE_ID;
}

function isScreenVisible(screenKey, user) {
  return !superAdminOnlyScreenKeys.has(screenKey) || isSuperAdmin(user);
}

module.exports = { isScreenVisible, isSuperAdmin, isWebAppUser, superAdminOnlyScreenKeys, SUPER_ADMIN_ROLE_ID };
