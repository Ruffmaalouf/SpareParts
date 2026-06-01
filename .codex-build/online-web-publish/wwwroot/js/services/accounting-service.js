function buildQuery(params) {
  const query = Object.entries(params)
    .filter(([, value]) => value !== undefined && value !== null && value !== "")
    .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(value)}`)
    .join("&");

  return query ? `?${query}` : "";
}

export class AccountingService {
  constructor(api) {
    this.api = api;
  }

  getAccounts() {
    return this.api.get("/api/accounting/accounts");
  }

  getStatementParties() {
    return this.api.get("/api/accounting/statement-parties");
  }

  getTrialBalance(dateFrom, dateTo) {
    return this.api.get(`/api/accounting/trial-balance${buildQuery({ dateFrom, dateTo })}`);
  }

  getLedger(accountId, dateFrom, dateTo) {
    return this.api.get(`/api/accounting/ledger${buildQuery({ accountId, dateFrom, dateTo })}`);
  }

  getStatementOfAccount(accountId, dateFrom, dateTo) {
    return this.api.get(`/api/accounting/statement-of-account${buildQuery({ accountId, dateFrom, dateTo })}`);
  }

  getStatementOfAccountForParty(partyType, partyId, dateFrom, dateTo) {
    return this.api.get(`/api/accounting/statement-of-account/party${buildQuery({
      partyType,
      partyId,
      dateFrom,
      dateTo
    })}`);
  }
}
