# TraduSaga — auditoria de prontidão SignPath

## Nota de escopo e atualização do checkout

Este arquivo foi trazido de uma preparação anterior. As seções abaixo com data de 2026-11-19 preservam o registro histórico e **não devem ser lidas como uma avaliação atual**: o commit indicado naquele relatório (`22b6bb37e686334f6768421973322269cab9b893`) não está disponível no histórico local deste checkout. A atualização local corrente está na branch `main`, commit `a9fbe37`; os documentos desta preparação já estão versionados.

### Verificações do checkout atual (2026-11-21)

| Item | Evidência local | Resultado atual |
|---|---|---|
| Target Windows | `src/TraduSaga/TraduSaga.csproj`: `net8.0-windows10.0.19041.0`, `win-x64` e `SelfContained=true` por padrão | O projeto declara target Windows 10.0.19041 e runtime x64. Isso não comprova teste em todas as versões do Windows. |
| Modelos | `src/TraduSaga/TraduSaga.csproj` procura `models/prediction/{chi,eng,jap,kor,rus}`; inventário em `models/prediction/README.md` | Os arquivos foram incluídos no Git LFS nesta preparação. O inventário registra a versão interna, os SHA-256 e a autorização de redistribuição informada pelo mantenedor; a origem upstream e uma licença separada continuam não identificadas. O caminho `artifacts/...` citado na análise histórica não corresponde ao `.csproj` atual. |
| Workflows | `.github/workflows/PR.check.yml` | Há validação de PR e `workflow_dispatch`, com restore bloqueado, auditoria de pacotes, testes e build. Não há workflow de release ou assinatura SignPath neste checkout. Isso não determina o estado de uma conta ou candidatura externa. |
| Documentação | `README.md`, `PRIVACY.md`, `SECURITY.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `docs/CODE-SIGNING-POLICY.md` | Os documentos foram adicionados ao Git. O README contém os links da release `v1.0.0` e o SHA-256 do ZIP. |
| Repositório e release | GitHub API: `maiconwiss/TraduSaga`, branch padrão `main` | O repositório está público; a release `v1.0.0` está publicada com o asset ZIP de 270.399.212 bytes. |
| Proteção do branch | GitHub API: `/branches/main/protection` | `main` exige PR, aplica as regras também ao administrador, bloqueia force-push e exclusão e exige resolução de conversas. São exigidas zero aprovações e não há status check obrigatório. |
| Segurança do GitHub | GitHub API: `security_and_analysis`, vulnerability alerts e private vulnerability reporting | Secret scanning, push protection, Dependabot security updates, Dependabot alerts e private vulnerability reporting estão habilitados. A habilitação não significa que os alertas foram revisados ou que não há achados. |
| Assinatura | `.github/workflows/PR.check.yml` | Não há workflow de assinatura SignPath no checkout. O dono informa que o projeto está em processo de submissão à Fundação. |

### Pendências que exigem confirmação do mantenedor

- O dono informou que o projeto está em processo de submissão à SignPath Foundation; o checkout ainda não tem workflow de assinatura.
- Confirmar com SignPath se o dono pode acumular os papéis de Author, Reviewer e Approver durante a submissão.
- A origem e a licença dos modelos locais não estão comprovadas no repositório.

O endereço oficial informado pelo dono foi aplicado aos links do app. A proteção de branch e as opções de segurança listadas acima foram conferidas diretamente nas configurações do GitHub; são configurações externas ao código versionado.

---

## Registro histórico da inspeção de 2026-11-19

O conteúdo restante foi mantido como histórico da análise anterior. Seus caminhos, tamanhos, disponibilidade de documentos, release, visibilidade e conclusões dependem do snapshot que ela examinou e não foram todos reproduzidos no checkout atual.

---

<!-- Relatório histórico original abaixo -->

**Data da inspeção:** 2026-11-19  
**Branch examinada:** `main`  
**Commit da árvore examinada:** `22b6bb37e686334f6768421973322269cab9b893`  
**Visibilidade observada:** privada  
**Escopo:** inspeção estática do repositório e leitura das políticas/documentação pública SignPath. Não foi possível compilar, executar ou escanear binários no ambiente do mantenedor.

## Conclusão executiva

**Estado: NÃO PRONTO PARA CANDIDATURA.**

Não foi encontrado indício confirmado de malware nos arquivos de código examinados. Há controles de segurança positivos: allowlist de hosts HTTPS, consentimento por provedor, limites de resposta HTTP, validação de hashes de DLLs nativas, DPAPI para chaves e workflow de PR com permissões mínimas e actions fixadas por SHA.

Entretanto, existem impedimentos e pendências importantes:
1. O repositório está privado. A política pública consultada da SignPath não afirma claramente, no texto disponível, se um repositório privado é aceito para a Fundação; isso precisa ser confirmado diretamente, sem alterar a visibilidade agora.
2. Não foi encontrado release publicado (`GET /releases` retornou lista vazia) nem workflow de release/build de artefatos.
3. Os cinco modelos de previsão esperados (`chi`, `eng`, `jap`, `kor`, `rus`) não estão versionados na árvore Git. O build espera arquivos locais em `artifacts/TraduSaga-1.0.1-win-x64-release/models/prediction`, mas `artifacts/` está ignorado pelo `.gitignore`.
4. As licenças de todos os pacotes transitivos, DLLs, ícones e modelos não foram integralmente verificadas. O `LICENSE` Apache 2.0 cobre o projeto, não automaticamente todo o conteúdo de terceiros.
5. O projeto inclui imagens com nomes `icons8-*`. A tela About examinada não apresenta atribuição visível a Icons8. A licença gratuita de Icons8 exige atribuição e impõe condições próprias; a licença/recibo específico dos arquivos usados não foi encontrado. Isso é uma pendência de licenciamento, não uma conclusão de infração.
6. O workflow atual valida PRs e compila, mas não publica artefatos, não gera hashes de release, não atesta procedência e não assina.

## Requisitos e evidências

| Requisito | Estado | Evidência / observação |
|---|---|---|
| Apache 2.0 presente | **Confirmado** | `LICENSE` contém Apache License 2.0; metadata GitHub informa SPDX `Apache-2.0`. O texto-modelo de copyright ainda contém placeholders. |
| Todas as dependências licenciadas | **Parcial** | Existem 8 arquivos `packages.lock.json`; versões estão bloqueadas. Licenças de todos os pacotes transitivos não foram auditadas individualmente. |
| Componentes proprietários | **Pendente** | Recursos `icons8-*`, DLLs nativas, modelos e demais assets exigem prova de licença/origem. |
| Malware/PUA | **Sem achado confirmado nos arquivos revistos** | Revisão estática seletiva; não é varredura AV dos binários nem auditoria exaustiva. |
| Downloads/execução remota | **Parcial** | Não foi observado download arbitrário no cliente HTTP examinado. O workflow baixa Gitleaks de URL fixa e confere SHA-256 antes de executar. Scripts de instalação de recursos Windows usam PowerShell local e cmdlets do Windows. |
| Transferência de dados | **Confirmado** | Google, DeepL e Yandex recebem texto para tradução após consentimento por provedor. |
| Alterações no Windows | **Confirmado** | OCR/TTS podem executar `Add-WindowsCapability` via PowerShell com elevação UAC. Interface pergunta antes de instalar. |
| Desinstalador | **N/A / não identificado** | Não foi encontrado projeto de instalador MSI/MSIX nem fluxo de instalação próprio. Se distribuição permanecer portátil, documentar remoção dos arquivos e dados locais. |
| Documentação de funcionalidades | **Insuficiente no estado do repositório** | README atual tem 435 bytes e cobre essencialmente modelos de previsão. |
| Binários rastreáveis ao source | **Não atendido** | Nenhum release encontrado; não há workflow de release nem artefatos publicados verificáveis. |
| Build/workflows seguros | **Parcialmente positivo** | Um workflow `PR.check.yml`, `contents: read`, checkout/setup-dotnet fixados por SHA, Gitleaks com SHA-256, restore `--locked-mode`, testes e build. |
| Metadados de versão | **Parcial** | `AssemblyInfo.cs` define Product/Title `TraduSaga`, Company e versões `1.0.0`; manifest usa assembly identity `1.0.0.0`. Ainda falta demonstrar que cada release injeta uma única versão coerente. |
| Governança Authors/Reviewers/Approvers | **Pendente** | Só um mantenedor principal é conhecido; não foi confirmada equipe independente nem permissões de revisão/aprovação. |
| MFA | **Não verificável** | Estado de MFA dos usuários não é visível no código. |
| Privacidade publicada | **Não atendido** | Não existia `PRIVACY.md` no repositório. Este pacote inclui um rascunho baseado no código examinado. |

## Achados confirmados

### 1. Rede e privacidade

`src/TraduSaga.Utils/Http/HttpReader.cs` limita solicitações a:
- `translate.googleapis.com`
- `clients5.google.com`
- `api-free.deepl.com`
- `api.deepl.com`
- `translate.api.cloud.yandex.net`

O cliente exige HTTPS na porta padrão, desabilita redirecionamentos automáticos, não usa cookies, limita solicitações a 20 segundos e respostas a 1 MiB.

`ProviderConsentService`, `WpfProviderConsentPrompt` e `ProviderConsentWindow` implementam consentimento por provedor. A interface oferece Allow, Deny e Cancel e apresenta links para as políticas dos três serviços. O estado inicial é `Unknown`.

DeepL e Yandex usam chaves de API. `DpapiSecretStore` grava segredos em `%LOCALAPPDATA%\\TraduSaga\\Secrets`, protegidos com DPAPI `CurrentUser`.

### 2. Alterações no Windows

`WindowsOCRHelper` e `WindowsTTSHelper`:
- validam idioma contra listas conhecidas;
- iniciam PowerShell do diretório de sistema;
- usam `-NoProfile` e `-NonInteractive`;
- solicitam elevação com `Verb = "runas"`;
- executam `Get-WindowsCapability` e `Add-WindowsCapability`.

A UI contém perguntas para instalar OCR/TTS e informa que a capacidade pode não estar instalada. Isso precisa constar no README/PRIVACY e ser testado em Windows limpo.

### 3. DLLs nativas

`native-library-manifest.json` registra:
- `OpenCvSharpExtern.dll` de `OpenCvSharp4.runtime.win` 4.7.0.20230115;
- `LdaNative.dll` de `Microsoft.ML` 2.0.1;
- hashes SHA-256 esperados.

`OpenCvNativeLibraryResolver` verifica caminho absoluto, ausência de reparse point e hash SHA-256 antes de carregar. Controle positivo relevante. Ainda falta comparar os hashes com os pacotes efetivamente restaurados no build de release.

### 4. Build

`.github/workflows/PR.check.yml`:
- roda em `pull_request` e `workflow_dispatch`;
- usa `permissions: contents: read`;
- fixa `actions/checkout` e `actions/setup-dotnet` em SHAs completos;
- baixa Gitleaks 8.30.1 e verifica SHA-256 antes de executar;
- executa `dotnet restore --locked-mode`;
- consulta pacotes vulneráveis;
- executa testes de segurança e build Release.

`Directory.Build.props` habilita `RestorePackagesWithLockFile`, `NuGetAudit`, modo `all` e trata NU1900–NU1904 como erros.

O workflow atual não usa secrets nem assina. O build e os testes passam `SkipPredictionModelCopy=true`; isso evita a cópia dos modelos e não produz, por si só, um pacote distribuível completo.

### 5. Modelos e assets

O `.csproj` do app espera cinco modelos em um diretório local `artifacts/.../models/prediction`. O `.gitignore` exclui `artifacts/`, e a árvore Git não contém esses modelos. Não foram encontrados manifestos com origem, licença, versão e hash dos modelos.

O app inclui recursos com nomes `icons8-high-priority-40.png` e `icons8-information-40.png`. A janela About examinada não contém crédito/link para Icons8. A licença gratuita de Icons8 exige atribuição; a titularidade/licença específica dos arquivos usados não foi verificada. Obter prova de licença adequada ou substituir por recursos sob licença OSI compatível antes de distribuir/assinar.

O arquivo `HammersmithOne-Regular.ttf` é associado à família Hammersmith One, cuja licença publicada é SIL Open Font License 1.1. Deve-se incluir o texto de licença e aviso de copyright da fonte na distribuição. Confirmar que o arquivo binário é a versão obtida sob OFL.

## Impedimentos para candidatura

**Bloqueadores técnicos/documentais:**
- Release distribuível e correspondente ao source ainda não demonstrado.
- Procedência/licença/hash dos modelos ausente.
- Licenças e origem de todos os assets e bibliotecas nativas não fechadas.
- Atribuição/licença Icons8 pendente.
- Política de privacidade final ainda não publicada/revisada.
- Workflow de release, hashes e vínculo entre tag/commit/artifact ausentes.
- Metadados de produto/versão precisam ser coerentes em todos os binários do pacote.

**Pontos que dependem de esclarecimento da SignPath:**
- Aceitação de repositório privado e forma de verificar controle/procedência.
- Acúmulo de Authors, Reviewers e Approvers por um único mantenedor.
- Número mínimo de aprovadores e possibilidade de autoaprovação pelo único mantenedor.
- Exigência de revisão independente quando há apenas um mantenedor.
- Como a Fundação avalia a reputação de um projeto novo, sem releases públicos prévios.

## Melhorias obrigatórias antes de candidatura

1. Resolver licenças de todos os componentes distribuídos, incluindo assets, fontes, DLLs e modelos; gerar SBOM com licença, versão, origem e hash.
2. Versionar ou obter os modelos de forma reproduzível e verificável; não depender de diretório local ignorado.
3. Produzir release a partir de tag/commit e workflow hospedado no GitHub, armazenar artefato do workflow e gerar SHA-256.
4. Revisar metadados de todos os executáveis e DLLs próprios.
5. Completar README, PRIVACY, SECURITY, CONTRIBUTING, CODE_OF_CONDUCT e política de assinatura.
6. Definir governança real, MFA e processo de revisão/aprovação.
7. Confirmar com SignPath as dúvidas sobre repositório privado e equipe de uma pessoa.

## Melhorias opcionais recomendadas

- Gerar SBOM CycloneDX/SPDX e anexá-lo ao release.
- Gerar provenance/attestation de build.
- Adicionar testes de integração para instalação opcional de capacidades Windows em VM descartável.
- Manter inventário de endpoints e dependências externas atualizado.
- Adicionar proteção de branch e CODEOWNERS para workflows/scripts de build quando houver revisores reais.

## Configuração futura SignPath — não ativar agora

Após aprovação formal, os valores abaixo deverão vir da própria conta/projeto SignPath:
- organization ID;
- project slug;
- signing policy slug;
- artifact configuration slug;
- API token armazenado como GitHub Secret;
- configuração de Trusted Build System GitHub;
- branch permitida para release;
- aprovadores e número de aprovações;
- regras de origem e restrições de metadados.

Não inserir placeholders como se fossem valores reais e não criar chamadas de API até a aprovação e configuração oficial.

## Checklist final

- [ ] SignPath confirmou elegibilidade de repositório privado ou mantenedor decidiu conscientemente quando torná-lo público.
- [ ] Apache 2.0 mantida e copyright notice preenchido.
- [ ] Todas as dependências e assets têm licenças compatíveis comprovadas.
- [ ] Ícones Icons8 licenciados/atribuídos corretamente ou substituídos.
- [ ] Modelos têm origem, versão, licença e SHA-256 verificáveis.
- [ ] Release funcional reproduzível a partir de tag/commit.
- [ ] Workflow de release usa permissões mínimas e actions fixadas por SHA.
- [ ] Artifacts e SHA-256 registrados por execução.
- [ ] Privacidade e mudanças no Windows explicadas ao usuário.
- [ ] Papéis Authors/Reviewers/Approvers confirmados com SignPath.
- [ ] MFA ativada para todos os membros.
- [ ] Aprovação manual para cada solicitação de assinatura.
- [ ] SignPath confirmou configuração e aprovou formalmente o projeto.

## Referências oficiais

- SignPath Foundation Terms: https://signpath.org/terms.html
- SignPath Documentation: https://docs.signpath.io/
- SignPath Projects: https://docs.signpath.io/projects
- SignPath GitHub Trusted Build System: https://docs.signpath.io/trusted-build-systems/github
- SignPath Signing Code: https://docs.signpath.io/signing-code
- SignPath Build System Integration: https://docs.signpath.io/build-system-integration
