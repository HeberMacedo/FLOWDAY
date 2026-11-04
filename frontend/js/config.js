// Endereço da API do FlowDay.
// - Com Docker Compose (padrão): http://localhost:8080
// - Com "dotnet run" (perfil de desenvolvimento): http://localhost:5080
// Para apontar para outro endereço sem editar este arquivo, defina
// window.FLOWDAY_API_URL antes de carregar os scripts.
export const API_URL = (window.FLOWDAY_API_URL || 'http://localhost:8080').replace(/\/$/, '');
