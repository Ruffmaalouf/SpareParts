const { webAppRoleId } = require("./app-config");

function isWebAppUser(user) {
  if (!user) return false;

  const roleId = Number(user.roleId ?? user.RoleId ?? user.roleID ?? user.role_id);
  if (roleId === webAppRoleId) {
    return true;
  }
  return false;
}

module.exports = { isWebAppUser };
