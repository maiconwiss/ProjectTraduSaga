# Como Contribuir

## Fluxo de contribuição

1. Abra uma issue para discutir mudanças relevantes antes de implementar funcionalidades grandes.
2. Crie uma branch de trabalho.
3. Mantenha alterações pequenas e com escopo claro.
4. Execute restore em modo bloqueado, build e testes de segurança.
5. Abra um pull request com motivação, resumo, riscos, testes executados e impacto sobre privacidade/segurança.
6. Não inclua binários, modelos ou ferramentas externas sem documentar origem, versão, hash e licença.

## Revisão

O workflow atual executa validações em pull requests sem expor secrets. Alterações em `.github/workflows`, scripts de build, projetos MSBuild, manifestos de dependências e código de download/execução exigem atenção reforçada.

O dono do projeto é Maicon Wissmann e acompanha as contribuições pelo GitHub. Não afirme que há revisão independente se ela não ocorreu. Contribuições de terceiros devem aguardar revisão por alguém autorizado pelo projeto antes de serem incorporadas. Os papéis de Authors, Reviewers e Approvers para assinatura ainda precisam ser confirmados com a SignPath Foundation.

## Dependências e modelos

- Fixe versões e mantenha os arquivos `packages.lock.json`.
- Não adicione feeds NuGet não aprovados.
- Não baixe ou execute binários externos sem origem confiável e verificação de integridade.
- Registre licença e atribuições de cada recurso distribuído.
- Modelos de previsão devem ter origem, versão, hash SHA-256 e licença identificáveis.

## Segurança e privacidade

Toda alteração que envia dados para fora do dispositivo, captura tela, modifica o Windows, solicita elevação ou instala componentes deve explicar o comportamento e preservar consentimento explícito.
