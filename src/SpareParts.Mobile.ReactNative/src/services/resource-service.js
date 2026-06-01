const { asRows } = require("../core/formatters");
const { buildPayload, rowId } = require("../admin/resource-utils");

class ResourceService {
  constructor(api) {
    this.api = api;
  }

  async list(endpoint) {
    return asRows(await this.api.list(endpoint));
  }
}

class CrudResourceService extends ResourceService {
  constructor(api, config) {
    super(api);
    this.config = config;
  }

  getId(row) {
    return rowId(row, this.config);
  }

  async save(row, form) {
    const isUpdate = Boolean(row);
    if (isUpdate && this.config.canUpdate === false) {
      throw new Error("Editing this resource is not exposed by the API.");
    }

    const payload = buildPayload(this.config, form, isUpdate);
    if (isUpdate) {
      await this.api.put(`${this.config.basePath}/${this.getId(row)}`, payload);
      return this.getId(row);
    }

    return this.api.post(this.config.basePath, payload);
  }

  async remove(row) {
    if (!row || this.config.canDelete === false) return null;

    await this.api.delete(`${this.config.basePath}/${this.getId(row)}`);
    return this.getId(row);
  }
}

module.exports = {
  CrudResourceService,
  ResourceService
};
