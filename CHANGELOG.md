# Changelog

Todas as mudanças notáveis deste projeto serão documentadas neste arquivo.

O formato é baseado no [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Versionamento Semântico](https://semver.org/lang/pt-BR/).

## [1.1.0] - 2026-10-06
### Adicionado
- Documentação inicial em português.
- Adicionado sistema de parser de arquivos `.twee` do Twine.
- Suporte a imagens, variáveis e múltiplas formatações de escolhas.
- Adicionada cena de exemplo demonstrando o funcionamento (`ExampleScene.unity`).

### Alterado
- Os diretórios padrão do sistema foram atualizados: arquivos `.twee` agora são carregados a partir de `TwineToUnity/Resources/Story` e as imagens de `TwineToUnity/Resources/image`.
- Melhoria no sistema de imagens da web: os downloads agora são salvos automaticamente (em cache) na pasta `image` local do projeto.
