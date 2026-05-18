const {
  CommunicationChannel,
  CommunicationRecipientKind,
  CommunicationTemplateKey
} = require("./app-config");

class CommunicationPayloadFactory {
  static salesInvoice(invoiceId) {
    return {
      channel: CommunicationChannel.WhatsApp,
      templateKey: CommunicationTemplateKey.SalesInvoice,
      recipientKind: CommunicationRecipientKind.Customer,
      salesInvoiceId: invoiceId
    };
  }

  static paymentReminder(invoiceId) {
    return {
      channel: CommunicationChannel.WhatsApp,
      templateKey: CommunicationTemplateKey.PaymentReminder,
      recipientKind: CommunicationRecipientKind.Customer,
      salesInvoiceId: invoiceId
    };
  }

  static partAvailability({ recipientName, recipientPhone, part }) {
    return {
      channel: CommunicationChannel.WhatsApp,
      templateKey: CommunicationTemplateKey.PartAvailability,
      recipientKind: CommunicationRecipientKind.Manual,
      recipientNameOverride: recipientName,
      recipientPhoneOverride: recipientPhone,
      partId: part.id,
      quotedPrice: part.salePrice
    };
  }

  static freeText({ recipientName, recipientPhone, body }) {
    return {
      channel: CommunicationChannel.WhatsApp,
      templateKey: CommunicationTemplateKey.FreeText,
      recipientKind: CommunicationRecipientKind.Manual,
      recipientNameOverride: recipientName,
      recipientPhoneOverride: recipientPhone,
      messageBody: body
    };
  }
}

module.exports = { CommunicationPayloadFactory };
