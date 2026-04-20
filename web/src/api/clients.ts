import { apiClient } from './client';

export interface Client {
  id: string;
  name: string;
  email: string;
  document: string;
  phone?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateClientInput {
  name: string;
  email: string;
  document: string;
  phone?: string | null;
}

export const clientsApi = {
  async list(skip = 0, take = 50): Promise<Client[]> {
    const { data } = await apiClient.get<Client[]>('/api/clients', {
      params: { skip, take },
    });
    return data;
  },

  async search(query: string, skip = 0, take = 20): Promise<Client[]> {
    const { data } = await apiClient.get<Client[]>('/api/clients/search', {
      params: { q: query, skip, take },
    });
    return data;
  },

  async getById(id: string): Promise<Client> {
    const { data } = await apiClient.get<Client>(`/api/clients/${id}`);
    return data;
  },

  async create(input: CreateClientInput): Promise<Client> {
    const { data } = await apiClient.post<Client>('/api/clients', input);
    return data;
  },

  async update(id: string, input: CreateClientInput): Promise<Client> {
    const { data } = await apiClient.put<Client>(`/api/clients/${id}`, input);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/api/clients/${id}`);
  },
};
