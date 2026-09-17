import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5243/api/products';

  getProducts(page: number = 1, limit: number = 10, search: string = '') {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('limit', limit.toString());
      
    if (search) {
      params = params.set('search', search);
    }
    
    return this.http.get<any>(this.apiUrl, { params });
  }

  getProduct(id: string) {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  createProduct(data: any) {
    return this.http.post<any>(this.apiUrl, data);
  }

  updateProduct(id: string, data: any) {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  deleteProduct(id: string) {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  uploadProductImage(id: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.apiUrl}/${id}/image`, formData);
  }
}
