const { webAppRoleId } = require("./app-config");
const SUPER_ADMIN_ROLE_ID = 5;

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

module.exports = { isSuperAdmin, isWebAppUser, SUPER_ADMIN_ROLE_ID };
