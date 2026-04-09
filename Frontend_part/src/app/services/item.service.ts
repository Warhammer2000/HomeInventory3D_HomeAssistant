import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {environment} from '../../environments/environment';
import {ItemDto, CreateItemDto, UpdateItemDto, UpdateItemStatusDto} from '../models/item.model';

@Injectable({providedIn: 'root'})
export class ItemService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/items`;

  getByContainer(containerId: string) {
    return this.http.get<ItemDto[]>(this.baseUrl, {params: {containerId}});
  }

  getById(id: string) {
    return this.http.get<ItemDto>(`${this.baseUrl}/${id}`);
  }

  search(query: string, limit = 20) {
    return this.http.get<ItemDto[]>(`${this.baseUrl}/search`, {params: {q: query, limit}});
  }

  create(dto: CreateItemDto) {
    return this.http.post<ItemDto>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateItemDto) {
    return this.http.put<ItemDto>(`${this.baseUrl}/${id}`, dto);
  }

  updateStatus(id: string, dto: UpdateItemStatusDto) {
    return this.http.patch<ItemDto>(`${this.baseUrl}/${id}/status`, dto);
  }

  delete(id: string) {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }
}
