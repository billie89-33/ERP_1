import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProductDto, PaginatedResponse, FilterOptionsDto, CategoryDto, FilterState } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = '/api/products';
  private catUrl = '/api/categories';

  getProducts(state: Partial<FilterState>): Observable<PaginatedResponse<ProductDto>> {
    let params = new HttpParams()
      .set('page', (state.page || 1).toString())
      .set('limit', (state.limit || 12).toString());
      
    if (state.searchQuery) params = params.set('search', state.searchQuery);
    if (state.categoryName && state.categoryName !== 'All') params = params.set('categoryName', state.categoryName);
    if (state.minPrice !== null && state.minPrice !== undefined) params = params.set('minPrice', state.minPrice.toString());
    if (state.maxPrice !== null && state.maxPrice !== undefined) params = params.set('maxPrice', state.maxPrice.toString());
    
    if (state.brands && state.brands.length > 0) {
      state.brands.forEach(brand => {
        params = params.append('brands', brand);
      });
    }

    if (state.selectedSpecs && Object.keys(state.selectedSpecs).length > 0) {
      params = params.set('specs', JSON.stringify(state.selectedSpecs));
    }
    
    return this.http.get<PaginatedResponse<ProductDto>>(this.apiUrl, { params });
  }

  getFilterOptions(categoryName?: string | null): Observable<FilterOptionsDto> {
    let params = new HttpParams();
    if (categoryName && categoryName !== 'All') {
      params = params.set('categoryName', categoryName);
    }
    return this.http.get<FilterOptionsDto>(`${this.apiUrl}/filters`, { params });
  }

  getCategories(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(this.catUrl);
  }

  getProduct(id: string): Observable<ProductDto> {
    return this.http.get<ProductDto>(`${this.apiUrl}/${id}`);
  }

  createProduct(data: Partial<ProductDto>) {
    return this.http.post<ProductDto>(this.apiUrl, data);
  }

  updateProduct(id: string, data: Partial<ProductDto>) {
    return this.http.put<ProductDto>(`${this.apiUrl}/${id}`, data);
  }

  deleteProduct(id: string) {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  uploadProductImage(id: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.apiUrl}/${id}/image`, formData);
  }
}
