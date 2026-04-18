import { api } from '@/lib/apiClient';

export const diagnosticoApi = {
  getAll:         ()         => api.get('/diagnostico'),
  getById:        (id)       => api.get(`/diagnostico/${id}`),
  lookup:         (whatsapp) => api.get(`/diagnostico/lookup?whatsapp=${encodeURIComponent(whatsapp)}`),
  create:         (data)     => api.post('/diagnostico', data),
  update:         (id, data) => api.put(`/diagnostico/${id}`, data),
  updateBloco6:   (id, data) => api.put(`/diagnostico/${id}/bloco6`, data),
  delete:          (id)       => api.delete(`/diagnostico/${id}`),
  editarCadastro:  (id, data) => api.patch(`/diagnostico/${id}/cadastro`, data),
  updateMentor:    (id, data) => api.put(`/diagnostico/${id}/mentor`, data),
  reenviarWpp:     (id)       => api.post(`/diagnostico/${id}/whatsapp/reenviar`),
  updateFase:      (id, fase) => api.patch(`/diagnostico/${id}/fase`, { fase }),
  criarConvidado:  (data)     => api.post('/diagnostico/convidado', data),
};
