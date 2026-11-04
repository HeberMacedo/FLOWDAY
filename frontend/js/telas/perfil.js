// Perfil e conta (UC10, RF09 a RF11).

import { api, atualizarUsuarioDaSessao, definirAviso, limparSessao, salvarSessao } from '../api.js';
import { carregando } from '../componentes.js';
import {
  ativarVerSenha, campoSenha, comCarregamento, confirmar, esc, formatarData, icone, limparErros,
  marcarErro, mostrarErros, toast, validarEmail, validarSenha
} from '../ui.js';

export async function renderizar(ctx) {
  const { el } = ctx;
  el.innerHTML = carregando();
  const usuario = await api.perfil.obter();
  ctx.atualizarUsuario(usuario);
  atualizarUsuarioDaSessao(usuario);

  el.innerHTML = `
    <div class="grade-perfil">
      <section class="cartao cartao-corpo">
        <h3 class="cartao-titulo">Dados pessoais</h3>
        <p class="cartao-sub">Membro desde ${esc(formatarData(usuario.criadoEm))}.</p>
        <form novalidate data-form-dados>
          <div data-alerta></div>
          <div class="campo">
            <label for="perfil-nome">Nome</label>
            <input class="entrada" id="perfil-nome" name="nome" maxlength="100" autocomplete="name" value="${esc(usuario.nome)}">
            <span class="erro-campo" data-erro="nome"></span>
          </div>
          <div class="campo">
            <label for="perfil-email">E-mail</label>
            <input class="entrada" type="email" id="perfil-email" name="email" maxlength="254" autocomplete="email" value="${esc(usuario.email)}">
            <span class="erro-campo" data-erro="email"></span>
          </div>
          <div class="oculto" data-senha-email>
            ${campoSenha({ nome: 'senhaAtual', id: 'dados-senha-atual', rotulo: 'Senha atual (obrigatória para trocar o e-mail)' })}
          </div>
          <button type="submit" class="btn btn-primario">Salvar dados</button>
        </form>
      </section>

      <section class="cartao cartao-corpo">
        <h3 class="cartao-titulo">Alterar senha</h3>
        <p class="cartao-sub">Ao trocar a senha, as sessões abertas em outros aparelhos são encerradas.</p>
        <form novalidate data-form-senha>
          <div data-alerta></div>
          ${campoSenha({ nome: 'senhaAtual', id: 'senha-atual', rotulo: 'Senha atual' })}
          ${campoSenha({ nome: 'novaSenha', rotulo: 'Nova senha', autocomplete: 'new-password', dica: '<span class="dica">Mínimo de 8 caracteres, com ao menos uma letra e um número.</span>' })}
          ${campoSenha({ nome: 'confirmacaoSenha', rotulo: 'Confirmar nova senha', autocomplete: 'new-password' })}
          <button type="submit" class="btn btn-primario">Alterar senha</button>
        </form>
      </section>

      <section class="cartao cartao-corpo perigo-zona">
        <h3 class="cartao-titulo">Excluir conta</h3>
        <p class="cartao-sub">Remove definitivamente sua conta, áreas, tarefas e itens de checklist. Essa ação não pode ser desfeita.</p>
        <form novalidate data-form-excluir class="form-inline">
          <div data-alerta style="flex-basis:100%"></div>
          ${campoSenha({ nome: 'senhaAtual', id: 'excluir-senha-atual', rotulo: 'Confirme com sua senha atual' })}
          <button type="submit" class="btn btn-perigo" style="align-self:flex-end">${icone('lixeira')}Excluir minha conta</button>
        </form>
      </section>
    </div>`;

  ativarVerSenha(el);
  ligarDados(el, usuario, ctx);
  ligarSenha(el);
  ligarExclusao(el);
}

/* RF09 e RN22: trocar o e-mail exige a senha atual. */
function ligarDados(el, usuario, ctx) {
  const form = el.querySelector('[data-form-dados]');
  const caixaSenha = form.querySelector('[data-senha-email]');
  let emailAtual = usuario.email;

  form.email.addEventListener('input', () => {
    const mudou = form.email.value.trim().toLowerCase() !== emailAtual.toLowerCase();
    caixaSenha.classList.toggle('oculto', !mudou);
  });

  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const dados = { nome: form.nome.value.trim(), email: form.email.value.trim(), senhaAtual: form.senhaAtual.value || null };
    const mudouEmail = dados.email.toLowerCase() !== emailAtual.toLowerCase();

    let valido = true;
    if (dados.nome.length < 2 || dados.nome.length > 100) { marcarErro(form, 'nome', 'O nome deve ter de 2 a 100 caracteres.'); valido = false; }
    const erroEmail = validarEmail(dados.email);
    if (erroEmail) { marcarErro(form, 'email', erroEmail); valido = false; }
    if (mudouEmail && !dados.senhaAtual) { marcarErro(form, 'senhaAtual', 'Informe a senha atual para trocar o e-mail.'); valido = false; }
    if (!valido) return;

    try {
      const atualizado = await comCarregamento(form.querySelector('[type=submit]'), () => api.perfil.atualizar(dados));
      emailAtual = atualizado.email;
      form.senhaAtual.value = '';
      caixaSenha.classList.add('oculto');
      atualizarUsuarioDaSessao(atualizado);
      ctx.atualizarUsuario(atualizado);
      toast('Dados atualizados.');
    } catch (erro) {
      mostrarErros(form, erro);
    }
  });
}

/* RF10 e RN05: a API devolve um novo token e invalida as demais sessões. */
function ligarSenha(el) {
  const form = el.querySelector('[data-form-senha]');
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const dados = { senhaAtual: form.senhaAtual.value, novaSenha: form.novaSenha.value, confirmacaoSenha: form.confirmacaoSenha.value };

    let valido = true;
    if (!dados.senhaAtual) { marcarErro(form, 'senhaAtual', 'Informe a senha atual.'); valido = false; }
    const erroSenha = validarSenha(dados.novaSenha);
    if (erroSenha) { marcarErro(form, 'novaSenha', erroSenha); valido = false; }
    if (dados.novaSenha !== dados.confirmacaoSenha) { marcarErro(form, 'confirmacaoSenha', 'A confirmação de senha não confere.'); valido = false; }
    if (!valido) return;

    try {
      const resposta = await comCarregamento(form.querySelector('[type=submit]'), () => api.perfil.alterarSenha(dados));
      salvarSessao(resposta);
      form.reset();
      toast('Senha alterada. As outras sessões foram encerradas.');
    } catch (erro) {
      mostrarErros(form, erro);
    }
  });
}

/* RF11 e RN21: senha atual + confirmação; remove todos os dados. */
function ligarExclusao(el) {
  const form = el.querySelector('[data-form-excluir]');
  form.addEventListener('submit', async (e) => {
    e.preventDefault();
    limparErros(form);
    const senhaAtual = form.senhaAtual.value;
    if (!senhaAtual) {
      marcarErro(form, 'senhaAtual', 'Informe a senha atual.');
      return;
    }
    const ok = await confirmar({
      titulo: 'Excluir sua conta?',
      mensagem: 'Todos os seus dados serão removidos definitivamente. Deseja continuar?',
      botao: 'Sim, excluir conta'
    });
    if (!ok) return;

    try {
      await comCarregamento(form.querySelector('[type=submit]'), () => api.perfil.excluir({ senhaAtual }));
      limparSessao();
      definirAviso('info', 'Sua conta foi excluída. Esperamos ver você de novo!');
      window.location.replace('index.html');
    } catch (erro) {
      mostrarErros(form, erro);
    }
  });
}
