import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AiStatusDto,
  ChatRequestDto,
  ChatResponseDto,
  DashboardAiBriefDto,
  DuplicateCheckRequestDto,
  DuplicateCheckResponseDto,
  SemanticProductMatchDto,
  SemanticSearchRequestDto,
  SyncEmbeddingsRequestDto,
  SyncEmbeddingsResponseDto
} from '../models/ai.model';

@Injectable({
  providedIn: 'root'
})
export class AiService {
  private http = inject(HttpClient);
  private apiUrl = '/api/ai';

  getStatus(): Observable<AiStatusDto> {
    return this.http.get<AiStatusDto>(`${this.apiUrl}/status`, { withCredentials: true });
  }

  syncEmbeddings(payload: SyncEmbeddingsRequestDto): Observable<SyncEmbeddingsResponseDto> {
    return this.http.post<SyncEmbeddingsResponseDto>(`${this.apiUrl}/sync-embeddings`, payload, { withCredentials: true });
  }

  semanticSearch(payload: SemanticSearchRequestDto): Observable<SemanticProductMatchDto[]> {
    return this.http.post<SemanticProductMatchDto[]>(`${this.apiUrl}/semantic-search`, payload);
  }

  checkDuplicate(payload: DuplicateCheckRequestDto): Observable<DuplicateCheckResponseDto> {
    return this.http.post<DuplicateCheckResponseDto>(`${this.apiUrl}/check-duplicate`, payload, { withCredentials: true });
  }

  storefrontChat(payload: ChatRequestDto): Observable<ChatResponseDto> {
    return this.http.post<ChatResponseDto>(`${this.apiUrl}/storefront-chat`, payload);
  }

  adminCopilot(payload: ChatRequestDto): Observable<ChatResponseDto> {
    return this.http.post<ChatResponseDto>(`${this.apiUrl}/admin-copilot`, payload, { withCredentials: true });
  }

  generateDashboardBrief(): Observable<DashboardAiBriefDto> {
    return this.http.post<DashboardAiBriefDto>(`${this.apiUrl}/dashboard-brief`, {}, { withCredentials: true });
  }
}
