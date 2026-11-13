export interface AiModuleCountDto {
  total: number;
  embedded: number;
  pending: number;
}

export interface AiStatusDto {
  apiKeyConfigured: boolean;
  embeddingModel: string;
  chatModel: string;
  products: AiModuleCountDto;
  salesOrders: AiModuleCountDto;
  purchaseOrders: AiModuleCountDto;
  suppliers: AiModuleCountDto;
  customers: AiModuleCountDto;
  totalRecords: number;
  totalEmbedded: number;
}

export interface SyncEmbeddingsRequestDto {
  forceRebuildAll: boolean;
  module?: string;
}

export interface SyncEmbeddingsResponseDto {
  message: string;
  productsSynced: number;
  salesOrdersSynced: number;
  purchaseOrdersSynced: number;
  suppliersSynced: number;
  customersSynced: number;
  totalSynced: number;
  failedCount: number;
  lastError?: string | null;
}

export interface SemanticSearchRequestDto {
  query: string;
  topK?: number;
  onlyInStock?: boolean;
  categoryName?: string | null;
}

export interface SemanticProductMatchDto {
  id: string;
  sku: string;
  name: string;
  brand: string;
  modelName: string;
  description: string;
  price: number;
  cost: number;
  onHandQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  categoryId: string;
  categoryName: string;
  imageUrl?: string | null;
  tags: string[];
  specifications?: Record<string, unknown>;
  similarityScore: number;
}

export interface DuplicateCheckRequestDto {
  name: string;
  brand?: string | null;
  modelName?: string | null;
  sku?: string | null;
  threshold?: number;
}

export interface DuplicateCheckResponseDto {
  hasDuplicates: boolean;
  matches: SemanticProductMatchDto[];
}

export interface ChatHistoryItemDto {
  role: 'user' | 'model';
  text: string;
}

export interface ChatRequestDto {
  message: string;
  history: ChatHistoryItemDto[];
}

export interface ChatDocumentRefDto {
  type: string;
  id: string;
  code: string;
  label: string;
  route: string;
  similarityScore: number;
}

export interface ChatResponseDto {
  reply: string;
  suggestedProducts: SemanticProductMatchDto[];
  referencedDocuments: ChatDocumentRefDto[];
  timestamp: string;
}

export interface DashboardAiBriefDto {
  financialSummary: string;
  urgentActions: string;
  restockRecommendations: string;
  executiveTakeaway: string;
  totalRevenue: number;
  totalExpense: number;
  netCashflow: number;
  pendingInboundPOs: number;
  pendingOutboundSOs: number;
  pendingSlipVerifications: number;
  generatedAt: string;
}
