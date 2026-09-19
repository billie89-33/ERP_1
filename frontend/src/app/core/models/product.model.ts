export interface ProductDto {
  id: string;
  sku: string;
  name: string;
  price: number;
  cost: number;
  stockQuantity: number;
  categoryId: string;
  categoryName: string;
  imageUrl?: string;
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
