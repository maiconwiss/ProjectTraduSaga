# Política de Privacidade — TraduSaga

**Status:** rascunho baseado em inspeção estática do código no checkout `main` em 2026-11-21 (commit `9b794fd0298cf330495fce65a3bd8d4859b376f4`). Deve ser revisado pelo mantenedor antes de ser tratado como declaração definitiva.

## Dados processados localmente

- O aplicativo captura a tela ou uma área selecionada para reconhecer texto.
- O reconhecimento OCR e o processamento auxiliar ocorrem no dispositivo, usando componentes locais/Windows.
- O aplicativo mantém configurações locais, preferências e cache de tradução.
- O aplicativo grava logs em `%LOCALAPPDATA%\TraduSaga\Logs`. Em builds Release, o arquivo `log.txt` recebe eventos de nível Warning ou superior, com rotação diária e retenção de até 10 arquivos. Em builds Debug, também é criado `trace.txt`, que recebe eventos Verbose e não tem limite de retenção configurado. A auditoria de todos os pontos de log ainda deve ser concluída para determinar se algum dado de texto pode ser registrado. Os logs permanecem após fechar o aplicativo; para removê-los, feche o aplicativo e exclua essa pasta.

## Dados enviados a serviços externos

O texto reconhecido pela captura/OCR pode ser enviado ao provedor de tradução selecionado. O código revisto contém os seguintes destinos:

| Provedor | Host(s) identificados | Dados enviados |
|---|---|---|
| Google | `translate.googleapis.com`, `clients5.google.com` | Texto a traduzir e códigos de idioma |
| DeepL | `api-free.deepl.com` ou `api.deepl.com` | Texto, idiomas e chave de API no cabeçalho de autorização |
| Yandex | `translate.api.cloud.yandex.net` | Texto, idiomas e chave de API no cabeçalho de autorização |

O aplicativo apresenta uma janela de consentimento específica para o provedor. A opção padrão é `Unknown`; sem autorização, o envio não deve prosseguir. O usuário pode permitir, negar ou cancelar e pode alterar/limpar o estado nas configurações. A janela de consentimento apresenta um link para a política de privacidade do provedor.

Políticas dos provedores:
- [Google Privacy Policy](https://policies.google.com/privacy)
- [DeepL Privacy Policy](https://www.deepl.com/privacy)
- [Yandex Privacy Policy](https://yandex.com/legal/confidential/)

A transmissão depende do provedor selecionado e da decisão do usuário. A política do provedor e os termos da conta/API também se aplicam ao conteúdo enviado.

## Chaves de API

As chaves DeepL e Yandex são armazenadas em arquivos locais sob `%LOCALAPPDATA%\TraduSaga\Secrets` e protegidas por Windows DPAPI com escopo `CurrentUser`. Elas são enviadas ao serviço correspondente apenas como credenciais de autorização da solicitação. Não inclua chaves de API em issues, logs, commits ou arquivos de configuração compartilhados.

## Alterações no Windows

O aplicativo pode oferecer a instalação de capacidades oficiais de idioma do Windows para OCR e TTS. O código usa `Get-WindowsCapability` e `Add-WindowsCapability` por PowerShell e solicita elevação via UAC (`runas`). A interface apresenta uma pergunta antes de instalar. A instalação é opcional e pode exigir componentes obtidos pelo próprio mecanismo de manutenção do Windows. O aplicativo não deve instalar essas capacidades silenciosamente.

## Telemetria

Na inspeção estática do código do aplicativo neste checkout, não foi identificado endpoint de telemetria próprio do TraduSaga. Isso não é uma garantia de ausência de telemetria em todos os componentes, dependências ou versões; a análise de dependências e binários distribuídos ainda está pendente.

## Verificação de atualizações

Em cada inicialização, o aplicativo consulta a API pública `https://api.github.com/repos/maiconwiss/TraduSaga/releases/latest` do GitHub para verificar se há uma versão estável mais recente. A solicitação transmite os metadados normais de conexão HTTP ao GitHub, como o endereço IP; não inclui texto reconhecido/capturado, configurações nem chaves de API. O aplicativo não baixa nem instala atualizações automaticamente. Se houver uma versão nova, o usuário pode abrir a página oficial de Releases para fazer o download manualmente.

## Retenção e exclusão

- O texto enviado a provedores externos fica sujeito às políticas e condições desses provedores.
- Os logs locais podem ser removidos pelo usuário após fechar o aplicativo e excluir a pasta `%LOCALAPPDATA%\TraduSaga\Logs`.
- As chaves armazenadas podem ser removidas pelas opções de exclusão nas configurações do aplicativo ou removendo os respectivos registros locais.
- A remoção das configurações locais deve ser documentada na versão distribuída.

## Contato e revisão

Esta política deve ser atualizada sempre que novos provedores, endpoints, telemetria, downloads, serviços ou integrações forem adicionados. A versão final deve incluir a data de vigência e indicar Maicon Wissmann como contato pelo GitHub.
