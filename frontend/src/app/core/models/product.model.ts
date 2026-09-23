export interface ProductDto {
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
  image?: {
    url: string;
    publicId: string;
  };
  tags: string[];
  status: string;
  isFeatured: boolean;
  soldCount: number;
  viewCount: number;
  specifications?: Record<string, any>;
}

export interface PaginatedResponse<T> {
  success: boolean;
  total: number;
  page: number;
  totalPages: number;
  data: T[];
}

export interface FilterOptionsDto {
  brands: string[];
  availableSpecs: Record<string, string[]>;
  minPrice: number;
  maxPrice: number;
}

export interface CategoryDto {
  id: string;
  name: string;
  description?: string;
}

export interface FilterState {
  categoryId: string | null;
  categoryName: string | null;
  searchQuery: string;
  brands: string[];
  minPrice: number | null;
  maxPrice: number | null;
  selectedSpecs: Record<string, string[]>;
  page: number;
  limit: number;
}
