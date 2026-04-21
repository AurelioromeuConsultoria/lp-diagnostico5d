import { api } from '@/lib/apiClient';

export const usersApi = {
  getAll:         ()         => api.get('/users'),
  create:         (data)     => api.post('/users', data),
  update:         (id, data) => api.put(`/users/${id}`, data),
  changePassword: (id, data) => api.patch(`/users/${id}/senha`, data),
  delete:         (id)       => api.delete(`/users/${id}`),
};
