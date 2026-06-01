export class CommunicationPayloadFactory {
  static salesInvoice(invoiceId) {
    return {
      channel: 0,
      templateKey: 0,
      recipientKind: 0,
      salesInvoiceId: invoiceId
    };
  }

  static paymentReminder(invoiceId) {
    return {
      channel: 0,
      templateKey: 2,
      recipientKind: 0,
      salesInvoiceId: invoiceId
    };
  }

  static partAvailability({ recipientName, recipientPhone, part }) {
    return {
      channel: 0,
      templateKey: 4,
      recipientKind: 2,
      recipientNameOverride: recipientName,
      recipientPhoneOverride: recipientPhone,
      partId: part.id,
      quotedPrice: part.salePrice
    };
  }

  static freeText({ recipientName, recipientPhone, body }) {
    return {
      channel: 0,
      templateKey: 6,
      recipientKind: 2,
      recipientNameOverride: recipientName,
      recipientPhoneOverride: recipientPhone,
      messageBody: body
    };
  }
}
