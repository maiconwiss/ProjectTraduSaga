# Code signing policy

**Status:** o projeto está em processo de submissão à SignPath Foundation. Ainda não há workflow de assinatura SignPath nos arquivos deste checkout.

## Situação atual

Não use a declaração “Free code signing provided by SignPath.io, certificate by SignPath Foundation” sem confirmação de aprovação formal e evidência de que o serviço está efetivamente configurado para o projeto.

## Escopo futuro

Somente binários próprios do TraduSaga, construídos a partir do código-fonte e scripts versionados do repositório, poderão ser submetidos para assinatura. Bibliotecas de terceiros podem integrar o pacote, mas não devem ser assinadas como se fossem código próprio do TraduSaga.

Cada versão deverá ter:
- tag e commit de origem identificáveis;
- workflow GitHub Actions associado;
- artefato não assinado armazenado como artifact do workflow;
- hash SHA-256 do artefato;
- verificação de origem/branch conforme política configurada na SignPath;
- metadados de produto e versão consistentes em todos os binários próprios;
- aprovação manual antes da assinatura de release.

## Papéis conhecidos

| Papel | Situação documentada |
|---|---|
| Authors | Maicon Wissmann, dono do projeto. |
| Reviewers | Não há revisor independente confirmado; o dono do projeto acompanha as mudanças. |
| Approvers | Maicon Wissmann, dono do projeto; validar com a SignPath se pode acumular este papel durante a submissão. |

Não inventar membros ou equipes. Atualizar esta seção somente após confirmar pessoas e permissões reais.

## Segurança da conta

Todos os membros que vierem a participar da equipe devem usar MFA no GitHub e na SignPath. Secrets e tokens deverão existir somente em GitHub Secrets/ambiente protegido, nunca no código ou nos artefatos.

## Configuração pendente

Após aprovação e criação da organização/projeto na SignPath, preencher apenas os identificadores e parâmetros reais fornecidos pela plataforma: organization ID, project slug, signing policy slug, artifact configuration e token de API. Restringir o build confiável a branches autorizadas e exigir aprovação manual para cada solicitação de assinatura de release.

## Declaração pós-aprovação

Somente após aprovação formal, inserir exatamente:

> Free code signing provided by SignPath.io, certificate by SignPath Foundation

## Privacidade

Consulte [../PRIVACY.md](../PRIVACY.md). A política final deverá também mencionar as políticas dos provedores externos de tradução usados pelo aplicativo.
