// Lógica das telas públicas: login, cadastro, recuperação e redefinição de senha (M1).

import { api, consumirAviso, definirAviso, limparSessao, obterSessao, salvarSessao } from './api.js';
import {
  ativarVerSenha, comCarregamento, esc, icone, limparErros, logo, marcarErro, mostrarErros,
  validarEmail, validarSenha
} from './ui.js';

const pagina = document.body.dataset.pagina;

montarPainelDaMarca();
ativarVerSenha();
mostrarAvisoPendente();

const iniciar = { login: iniciarLogin, cadastro: iniciarCadastro, recuperar: iniciarRecuperacao, redefinir: iniciarRedefinicao };
iniciar[pagina]?.();

/* ---------------- Login (UC02) ---------------- */

function iniciarLogin() {
  if (obterSessao()) {
    window.location.replace('app.html');
    return;
  }

  const form = document.querySelector('#form-login');
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const email = form.email.value.trim();
    const senha = form.senha.value;

    let valido = true;
    const erroEmail = validarEmail(email);
    if (erroEmail) { marcarErro(form, 'email', erroEmail); valido = false; }
    if (!senha) { marcarErro(form, 'senha', 'Informe a senha.'); valido = false; }
    if (!valido) return;

    try {
      const resposta = await comCarregamento(form.querySelector('[type=submit]'), () => api.auth.entrar({ email, senha }));
      salvarSessao(resposta);
      window.location.replace('app.html');
    } catch (erro) {
      // Mensagem genérica para credenciais (RN04); bloqueio e excesso de tentativas aparecem como aviso.
      const tipo = erro.status === 423 || erro.status === 429 ? 'aviso' : 'erro';
      mostrarAlerta(form, tipo, erro.message);
      form.senha.focus();
    }
  });
}

/* ---------------- Cadastro (UC01) ---------------- */

function iniciarCadastro() {
  if (obterSessao()) {
    window.location.replace('app.html');
    return;
  }

  const form = document.querySelector('#form-cadastro');
  acompanharRequisitosSenha(form.senha, form.querySelector('.requisitos-senha'));

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const dados = {
      nome: form.nome.value.trim(),
      email: form.email.value.trim(),
      senha: form.senha.value,
      confirmacaoSenha: form.confirmacaoSenha.value
    };

    const erros = {
      nome: dados.nome.length < 2 || dados.nome.length > 100 ? 'O nome deve ter de 2 a 100 caracteres.' : null,
      email: validarEmail(dados.email),
      senha: validarSenha(dados.senha),
      confirmacaoSenha: dados.senha !== dados.confirmacaoSenha ? 'A confirmação de senha não confere.' : null
    };
    const invalidos = Object.entries(erros).filter(([, msg]) => msg);
    if (invalidos.length) {
      invalidos.forEach(([campo, msg]) => marcarErro(form, campo, msg));
      form.querySelector('.invalida')?.focus();
      return;
    }

    try {
      const resposta = await comCarregamento(form.querySelector('[type=submit]'), () => api.auth.cadastrar(dados));
      salvarSessao(resposta);
      definirAviso('sucesso', `Conta criada! Boas-vindas ao FlowDay, ${resposta.usuario.nome.split(' ')[0]}.`);
      window.location.replace('app.html');
    } catch (erro) {
      mostrarErros(form, erro);
    }
  });
}

/* ---------------- Esqueci minha senha (UC03) ---------------- */

function iniciarRecuperacao() {
  const form = document.querySelector('#form-recuperar');
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const email = form.email.value.trim();
    const erroEmail = validarEmail(email);
    if (erroEmail) {
      marcarErro(form, 'email', erroEmail);
      return;
    }

    try {
      const resposta = await comCarregamento(form.querySelector('[type=submit]'), () => api.auth.recuperarSenha({ email }));
      // A resposta é a mesma, exista ou não o e-mail (RN06).
      document.querySelector('#etapa-formulario').classList.add('oculto');
      const concluido = document.querySelector('#etapa-concluida');
      concluido.querySelector('[data-mensagem]').textContent = resposta.mensagem;
      concluido.querySelector('[data-email]').textContent = email;
      concluido.classList.remove('oculto');
    } catch (erro) {
      mostrarErros(form, erro);
    }
  });

  document.querySelector('#reenviar')?.addEventListener('click', () => {
    document.querySelector('#etapa-concluida').classList.add('oculto');
    document.querySelector('#etapa-formulario').classList.remove('oculto');
  });
}

/* ---------------- Redefinir senha pelo link (UC03) ---------------- */

function iniciarRedefinicao() {
  const form = document.querySelector('#form-redefinir');
  const token = new URLSearchParams(window.location.search).get('token');

  if (!token) {
    form.classList.add('oculto');
    document.querySelector('#link-invalido').classList.remove('oculto');
    return;
  }

  acompanharRequisitosSenha(form.novaSenha, form.querySelector('.requisitos-senha'));

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const dados = { token, novaSenha: form.novaSenha.value, confirmacaoSenha: form.confirmacaoSenha.value };

    const erroSenha = validarSenha(dados.novaSenha);
    if (erroSenha) { marcarErro(form, 'novaSenha', erroSenha); return; }
    if (dados.novaSenha !== dados.confirmacaoSenha) {
      marcarErro(form, 'confirmacaoSenha', 'A confirmação de senha não confere.');
      return;
    }

    try {
      const resposta = await comCarregamento(form.querySelector('[type=submit]'), () => api.auth.redefinirSenha(dados));
      // Sessões anteriores deixam de valer (RN05), inclusive a deste navegador.
      limparSessao();
      definirAviso('sucesso', resposta.mensagem);
      window.location.replace('index.html');
    } catch (erro) {
      if (erro.status === 422) {
        form.querySelector('[data-alerta]').innerHTML = `<div class="alerta alerta-erro" role="alert">${icone('alerta')}
          <span>${esc(erro.message)} <a href="recuperar-senha.html">Solicitar novo link</a></span></div>`;
      } else {
        mostrarErros(form, erro);
      }
    }
  });
}

/* ---------------- Apoio ---------------- */

function mostrarAlerta(form, tipo, texto) {
  const icones = { erro: 'alerta', aviso: 'triangulo', sucesso: 'sucesso', info: 'info' };
  form.querySelector('[data-alerta]').innerHTML =
    `<div class="alerta alerta-${tipo}" role="alert">${icone(icones[tipo])}<span>${esc(texto)}</span></div>`;
}

function mostrarAvisoPendente() {
  const aviso = consumirAviso();
  const alvo = document.querySelector('form [data-alerta]');
  if (aviso && alvo) mostrarAlerta(alvo.closest('form'), aviso.tipo, aviso.texto);
}

/** Indica em tempo real os requisitos da senha (RN03, RNF13). */
function acompanharRequisitosSenha(entrada, lista) {
  if (!entrada || !lista) return;
  const regras = [
    ['tamanho', (s) => s.length >= 8],
    ['letra', (s) => /\p{L}/u.test(s)],
    ['numero', (s) => /\d/.test(s)]
  ];
  const atualizar = () => regras.forEach(([nome, teste]) =>
    lista.querySelector(`[data-regra="${nome}"]`).classList.toggle('ok', teste(entrada.value)));
  entrada.addEventListener('input', atualizar);
  atualizar();
}

function montarPainelDaMarca() {
  document.querySelectorAll('[data-logo]').forEach((el) => { el.innerHTML = logo(); });
  const painel = document.querySelector('[data-marca]');
  if (!painel) return;
  painel.innerHTML = `
    <div>
      <a class="logo" href="index.html">${logo()}</a>
      <h1>Seu dia em fluxo, sem esquecer nada.</h1>
      <p class="lema">Tarefas pessoais, de trabalho, estudos e lazer em um só lugar, com checklist, prioridades por cor e prazos sob controle.</p>
      <ul>
        <li><span class="ic">${icone('lista')}</span>Checklist com progresso em cada tarefa</li>
        <li><span class="ic">${icone('kanban')}</span>Lista, Kanban e calendário com os mesmos filtros</li>
        <li><span class="ic">${icone('triangulo')}</span>Tarefas atrasadas destacadas automaticamente</li>
        <li><span class="ic">${icone('escudo')}</span>Seus dados protegidos e visíveis só para você</li>
      </ul>
      <div class="previa">
        <span class="selo selo-prioridade" style="--cor:#22C55E">Baixa</span>
        <span class="selo selo-prioridade" style="--cor:#EAB308">Média</span>
        <span class="selo selo-prioridade" style="--cor:#F97316">Alta</span>
        <span class="selo selo-prioridade" style="--cor:#EF4444">Urgente</span>
      </div>
    </div>
    <footer>Projeto acadêmico · Engenharia de Software · CEUB</footer>`;
}
