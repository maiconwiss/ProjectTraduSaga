# Instruções para agentes

- Leia `README.md` e `CONTRIBUTING.md` antes de mudanças amplas; consulte `PRIVACY.md` e `SECURITY.md` para alterações relacionadas.
- Mantenha as mudanças pequenas e focadas. Siga `.editorconfig` e os padrões C#/.NET 8 existentes.
- Preserve `packages.lock.json`; restaure dependências com `dotnet restore TraduSaga.sln --locked-mode`. Não adicione feeds NuGet sem aprovação do projeto.
- Valide mudanças de código com os comandos do CI: `dotnet test tests\TraduSaga.Security.Tests\TraduSaga.Security.Tests.csproj --configuration Release --no-restore -p:SkipPredictionModelCopy=true` e `dotnet build TraduSaga.sln --configuration Release --no-restore -p:SkipPredictionModelCopy=true`.
- Não adicione binários, modelos ou ferramentas externas sem documentar origem, versão, hash SHA-256 e licença. Não execute downloads sem origem confiável e verificação de integridade.
- Preserve consentimento explícito e minimize dados enviados para fora do dispositivo. Nunca inclua segredos, credenciais ou dados pessoais em código, logs ou documentação.
- Trate mudanças em `.github/workflows`, scripts de build, projetos MSBuild, dependências e código de download ou execução como sensíveis à segurança; revise também seus efeitos indiretos.
- Ao concluir, informe o que mudou, quais validações executou e eventuais riscos ou limitações conhecidos.

## Processo de release para Windows x64

O merge de uma PR na `main` não publica uma versão. Este repositório não possui workflow automático de release; a publicação é manual e só deve acontecer depois que a preparação da versão estiver revisada e mesclada.

1. Confirme que a `main` está atualizada e limpa e que as PRs da versão foram mescladas. Baixe os objetos LFS com `git lfs pull`; confira que os cinco modelos (`chi`, `eng`, `jap`, `kor`, `rus`) estão presentes e valide seus hashes com `models/prediction/README.md`.
2. Crie uma branch de preparação da versão. Atualize em conjunto `AssemblyFileVersion`, `AssemblyInformationalVersion` e `AssemblyVersion` em `src/TraduSaga/AssemblyInfo.cs`, além do `assemblyIdentity` em `src/TraduSaga/app.manifest`. A janela Sobre lê a versão do executável; valide que `FileVersion` e `ProductVersion` exibem a versão nova.
3. Restaure dependências bloqueadas e valide o código com os comandos usados no CI:

   ```powershell
   dotnet restore TraduSaga.sln --locked-mode
   dotnet test tests\TraduSaga.Security.Tests\TraduSaga.Security.Tests.csproj --configuration Release --no-restore -p:SkipPredictionModelCopy=true
   dotnet build TraduSaga.sln --configuration Release --no-restore -p:SkipPredictionModelCopy=true
   ```

   Os testes de proteção de dados usam DPAPI `CurrentUser`; execute-os com o perfil Windows do usuário carregado. Se falharem por perfil indisponível no sandbox, repita no ambiente Windows normal e registre o resultado; não desative esses testes para liberar a versão.
4. Depois do merge dos metadados na `main`, publique o app autocontido e x64. Use uma pasta de saída nova para a versão, evitando misturar arquivos antigos:

   ```powershell
   $version = '1.0.2'
   $publishDir = Join-Path (Get-Location) "artifacts/TraduSaga-$version-win-x64"
   dotnet publish src\TraduSaga\TraduSaga.csproj --configuration Release --runtime win-x64 --self-contained true --no-restore "-p:PublishDir=$publishDir\"
   ```

   Confira que o diretório contém `TraduSaga.exe`, `OpenCvSharpExtern.dll`, `LdaNative.dll` e os cinco arquivos em `models/prediction`. Compare os hashes dos modelos com o inventário, e confirme a versão do executável antes de compactar.
5. Crie `TraduSaga-$version-win-x64.zip` a partir do conteúdo da pasta publicada, calcule seu SHA-256 e registre o tamanho. Não substitua o hash por um hash do executável ou por um valor de build anterior.
6. Crie uma tag anotada `v$version` no commit validado da `main` e envie-a ao GitHub. Crie uma GitHub Release para essa tag, anexe o ZIP, inclua notas de versão e o SHA-256 e publique a release. Verifique na página pública o tag, o arquivo, o tamanho e o digest SHA-256. Se não puder validar o digest remoto, não declare a publicação verificada.
7. Atualize os links de Downloads e o SHA-256 em `README.md` usando o artefato publicado; envie essa alteração por PR e confirme que o link da release e o link direto do ZIP abrem corretamente.

Não publique binários com modelos cuja procedência/direitos não estejam documentados. Não inclua tokens ou credenciais em scripts, comandos registrados ou logs. A assinatura de código só pode ser anunciada quando existir uma configuração e uma assinatura comprováveis.
