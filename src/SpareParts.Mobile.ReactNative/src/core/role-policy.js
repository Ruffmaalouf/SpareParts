const { webAppRoleId, webAppRoleName } = require("./app-config");

function isWebAppUser(user) {
  if (!user) return false;

  const roleId = Number(user.roleId ?? user.RoleId ?? user.roleID ?? user.role_id);
  if (roleId === webAppRoleId) {
    return true;
  }

  return String(user.role || user.Role || "").trim().toLowerCase() === webAppRoleName.toLowerCase();
}

module.exports = { isWebAppUser };
