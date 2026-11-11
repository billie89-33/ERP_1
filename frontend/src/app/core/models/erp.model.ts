export interface ApiMessageResponse {
  message?: string;
  poId?: string;
  poNumber?: string;
  soId?: string;
  OrderNumber?: string;
}

export interface AuthUser {
  id: string;
  username: string;
  role: string;
  firstName?: string;
  lastName?: string;
  email?: string;
}

export interface LoginCredentials {
  username?: string;
  email?: string;
  password?: string;
}

export interface AuthLoginResponse {
  user: AuthUser;
  token?: string;
  message?: string;
}

export interface StorefrontUser {
  id?: string;
  firstName?: string;
  lastName?: string;
  email?: string;
  phone?: string;
  address?: string;
  role?: string;
  username?: string;
}

export interface StorefrontRegisterData {
  username?: string;
  firstName?: string;
  lastName?: string;
  email: string;
  password?: string;
  phone?: string;
  address?: string;
}

export interface StorefrontAuthResponse {
  user: StorefrontUser;
  token?: string;
  message?: string;
}

export interface CheckoutResponse {
  orderId: string;
  orderNumber: string;
  totalAmount?: number;
  message?: string;
}

export interface FeaturedProductDto {
  id: string;
  name: string;
  price: number;
  imageUrl: string | null;
  categoryName: string;
}

export interface DashboardChartPointDto {
  date: string;
  amount: number;
}

export interface DashboardLowStockProductDto {
  sku: string;
  name: string;
  onHandQuantity: number;
}

export interface DashboardEventDto {
  id: string;
  timestamp: string;
  eventId: string;
  type: string;
  details: string;
  status: string;
}

export interface DashboardStatsDto {
  totalSales: number;
  pendingSalesOrders: number;
  pendingSlipVerifications: number;
  pendingPurchaseOrders: number;
  lowStockProducts: DashboardLowStockProductDto[];
  chartData: DashboardChartPointDto[];
  recentEvents: DashboardEventDto[];
}

export interface PaginatedResult<T> {
  items: T[];
  data?: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CustomerDto {
  id?: string;
  customerType: string;
  companyName: string;
  firstName?: string;
  lastName?: string;
  taxId?: string;
  address?: string;
  phone?: string;
  email?: string;
  creditTermDays: number;
  creditLimit: number;
}

export interface SupplierDto {
  id?: string;
  companyName: string;
  taxId: string;
  contactName?: string;
  phone?: string;
  email?: string;
  address?: string;
  paymentTerms?: string;
  creditTermDays?: number;
}

export interface UserDto {
  id?: string;
  username: string;
  password?: string;
  newPassword?: string;
  role: string;
  firstName?: string;
  lastName?: string;
  email?: string;
}

export interface CompanySettingsDto {
  companyName: string;
  taxId?: string;
  address?: string;
  phone?: string;
  email?: string;
  logoUrl?: string;
  bankName?: string;
  bankAccountName?: string;
  bankAccountNumber?: string;
  promptPayQrUrl?: string;
}

export interface PurchaseOrderItemDto {
  productId: string;
  productName: string;
  quantity: number;
  unitCost: number;
  actualReceivedQty: number;
}

export interface PurchaseOrderDto {
  id: string;
  poNumber: string;
  orderDate: string;
  status: string;
  supplierId?: string;
  supplierName: string;
  createdByName?: string;
  expectedDeliveryDate?: string;
  remarks?: string;
  referenceNumber?: string;
  paymentTerms?: string;
  subTotal?: number;
  vatRate?: number;
  taxAmount?: number;
  totalAmount: number;
  items: PurchaseOrderItemDto[];
}

export interface SalesOrderItemDto {
  productId: string;
  productName: string;
  productImage?: string;
  productImageUrl?: string;
  quantity: number;
  unitPrice: number;
}

export interface SalesOrderDto {
  id: string;
  orderNumber: string;
  orderDate: string;
  status: string;
  paymentStatus: string;
  paymentSlipUrl?: string;
  orderChannel?: string;
  totalAmount: number;
  customerName: string;
  customerEmail?: string;
  createdByName?: string;
  courier?: string;
  trackingNumber?: string;
  expiresAt?: string;
  shippingAddress?: string;
  customerPhone?: string;
  bankName?: string;
  bankAccountName?: string;
  bankAccountNumber?: string;
  promptPayQrUrl?: string;
  items: SalesOrderItemDto[];
}

export interface GoodsIssueItemForm {
  productId: string;
  productName: string;
  orderedQuantity: number;
  issueQuantity: number;
}

export interface GoodsReceiptItemForm {
  productId: string;
  productName: string;
  orderedQuantity: number;
  receiveQuantity: number;
}
