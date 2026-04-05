namespace StockFlowPro.Domain.Enums;

public enum Currency { DZD, EUR, USD, MAD }
public enum UserRole { SuperAdmin, TenantAdmin, Manager, Cashier }
public enum OrderStatus { Draft, Confirmed, Shipped, Delivered, Cancelled }
public enum InvoiceStatus { Draft, Sent, Paid, Overdue, Cancelled }
public enum StockMovementType { In, Out, Adjustment, Return }
public enum PaymentMethod { Cash, BankTransfer, CreditCard, Check, Other }
public enum AppLanguage { FR, EN, AR }
public enum SubscriptionMode { Local, Cloud }
