// Endereço da API do FlowDay.
// - Com Docker Compose (padrão): http://localhost:8080
// - Com "dotnet run" (perfil de desenvolvimento): http://localhost:5080
// Para apontar para outro endereço sem editar este arquivo, defina
// window.FLOWDAY_API_URL antes de carregar os scripts.
export const API_URL = (window.FLOWDAY_API_URL || 'http://localhost:8080').replace(/\/$/, '');

// No GitHub Pages não existe servidor para a API em C# nem para o MySQL. Lá, o front-end
// entra em modo demonstração (js/demo.js): as mesmas rotas e regras, com os dados no navegador.
export const MODO_DEMO = window.FLOWDAY_DEMO ?? window.location.hostname.endsWith('github.io');

export const URL_REPOSITORIO = 'https://github.com/HeberMacedo/FLOWDAY';
