# Twine To Unity

Um pacote para Unity que permite importar, interpretar e reproduzir histórias interativas e diálogos criados no formato `.twee` (Twine/Harlowe). 
O sistema analisa passagens, escolhas, variáveis (condições e comandos), além de suportar imagens, facilitando a integração de narrativas ramificadas em jogos feitos na Unity.

## Funcionalidades

- **Parser de Arquivos `.twee`**: Lê e interpreta nativamente arquivos com a formatação padrão do Twine (cabeçalhos `::`).
- **Navegação e Escolhas**: Suporte para diversos formatos de links (`[[Destino]]`, `[[Texto|Destino]]`, `[[Texto->Destino]]`).
- **Condições e Variáveis**: Lida com variáveis (`SetCommands`) e lógica condicional nas escolhas para adaptar a história de acordo com o progresso do jogador.
- **Suporte a Imagens**: Extrai e exibe imagens referenciadas com a sintaxe `[[Image:nome_da_imagem]]`.
- **Extração de Metadados**: Coleta posições, tamanhos e tags das passagens definidos no editor Twine, permitindo integrações customizadas (ex: nós visuais no editor).
- **Exemplos Prontos para Uso**: Acompanha scripts de contêiner de diálogo (`DialogueContainer.cs`), prefabs e uma cena de exemplo (`Examples/ExampleScene.unity`) demonstrando o fluxo funcionando.

## Instalação

### Instalação

1. Baixe ou clone este repositório.
2. Copie a pasta `Assets/TwineToUnity` e cole diretamente dentro da pasta `Assets` do seu projeto Unity.

*Desta forma, os arquivos estarão presentes no diretório `Assets` ao invés de `Packages`, permitindo acesso total e edição dos recursos, prefabs e exemplos de forma direta.*

## Como Usar

1. **Estrutura de Pastas**: Certifique-se de colocar as pastas no seu projeto como `TwineToUnity/Resources/Story` (para as histórias) e `TwineToUnity/Resources/image` (para as imagens).
2. **Importe o seu arquivo Twee**: Coloque seu arquivo `.twee` dentro da pasta `TwineToUnity/Resources/Story`. O script buscará a história neste diretório.
3. **Imagens Locais e Web**:
   - Arquivos locais referenciados com `[[Image:nome.png]]` devem ficar na pasta `TwineToUnity/Resources/image`.
   - Se a imagem for um link web (ex: `[[Image:http...]]`), o sistema fará o download e vai **salvar automaticamente** (cache) na sua pasta `image` local.
4. **Cena e Prefabs**: Adicione o Prefab contido em `Prefabs/` na sua cena.
5. **Configure a Interface**: O sistema vem pronto. Apenas digite o nome do seu arquivo `.twee` no componente de script e teste!
6. **Para Aprender**: Abra e teste a cena `Examples/ExampleScene.unity` para ver o fluxo em funcionamento.

## Requisitos
- **Unity 2021.3** ou superior (conforme `package.json`).

## Licença

Este projeto é distribuído sob a Licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.
