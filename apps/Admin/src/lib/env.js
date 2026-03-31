// Em dev: requests vão para /api → Vite proxy → http://localhost:5000/api
// Em prod: requests vão para /api → .NET serve direto (mesmo origin)
export const API_BASE_URL_WITH_API = '/api';
