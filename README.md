# TraduSaga

Projeto de tradução de RPGs. O TraduSaga é um aplicativo Windows em C#/.NET que reconhece texto exibido na tela e apresenta sua tradução em uma janela sobreposta. Inclui captura de tela, reconhecimento óptico de caracteres (OCR), tradução por provedores externos selecionáveis e leitura em voz alta opcional.

**Assinatura:** o projeto está em processo de submissão à SignPath Foundation. Não há workflow de assinatura SignPath neste checkout; atualizaremos esta informação quando a submissão avançar e houver configuração ou aprovação confirmada.

## Funcionalidades

- Captura contínua da tela ou de uma área selecionada.
- Reconhecimento de texto por mecanismos OCR, incluindo Windows OCR e componentes OpenCV.
- Processamento local auxiliar para avaliar e filtrar o texto reconhecido.
- Tradução usando Google Translate, DeepL ou Yandex, conforme configuração e consentimento.
- Janela de tradução sobreposta, atalhos e suporte a entrada por controle.
- Leitura em voz alta usando recursos de TTS do Windows.

## Requisitos

- Windows 10 versão 2004 (build 19041) ou posterior, conforme o target do projeto.
- Arquitetura Windows x64.
- O pacote publicado deve incluir os modelos de previsão e as bibliotecas nativas necessárias.
- Recursos de idioma OCR/TTS do Windows podem ser necessários conforme os idiomas escolhidos.

## Instalação e utilização

Este checkout não documenta um release oficial verificado nem fornece um link de download. Use apenas canais oficiais confirmados pelo mantenedor.

Quando uma distribuição oficial estiver disponível, esta seção deverá apontar para o release exato, listar seu SHA-256 e explicar se o pacote é portátil ou usa instalador.

A utilização prevista é selecionar os idiomas de origem e destino, escolher um provedor de tradução, conceder ou negar o envio de texto a esse provedor, configurar OCR/TTS e iniciar a captura e tradução. A interface pode solicitar autorização do Windows para instalar pacotes de idioma OCR/TTS; essa operação instala capacidades oficiais do Windows e requer elevação.

## Modelos de previsão

O build usa os cinco modelos locais `chi`, `eng`, `jap`, `kor` e `rus` armazenados em `models/prediction` na raiz do projeto e os publica ao lado do aplicativo. Para usar outro diretório, informe `-p:TraduSagaPredictionModelsPath="CAMINHO\PARA\prediction"` ao executar `dotnet build` ou `dotnet publish`.

## Privacidade

Consulte [PRIVACY.md](PRIVACY.md). Em resumo, o OCR e a captura são realizados localmente; quando o usuário permite um provedor de tradução, o texto reconhecido é enviado ao serviço correspondente. O código revisado contém consentimento por provedor e permite alterar ou retirar essa permissão nas configurações.

## Segurança

Consulte [SECURITY.md](SECURITY.md) para reportar vulnerabilidades de forma responsável.

## Licença

O repositório contém Apache License 2.0. A licença do código-fonte não substitui as licenças de fontes, ícones, modelos, bibliotecas nativas e dependências de terceiros. Consulte o relatório [docs/SIGNPATH-READINESS.md](docs/SIGNPATH-READINESS.md).

## Assinatura de código

Não há workflow de assinatura SignPath no repositório deste checkout. O projeto está em processo de submissão; não descreva executáveis como assinados pela SignPath sem evidência da assinatura.

Após aprovação formal e configuração efetiva, atualize esta seção com a declaração aplicável e os responsáveis reais por autoria, revisão e aprovação, conforme orientação da Fundação. Consulte [docs/CODE-SIGNING-POLICY.md](docs/CODE-SIGNING-POLICY.md).

## Desenvolvimento

Consulte [CONTRIBUTING.md](CONTRIBUTING.md). O build usa .NET 8 e os arquivos `packages.lock.json` devem ser restaurados em modo bloqueado (`--locked-mode`).

## Downloads

Não há URL de download oficial verificado documentado neste checkout. Confirme a existência de releases e o canal oficial antes de publicar instruções de download.
