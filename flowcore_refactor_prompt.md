# Prompt de Refatoração — SuperMediaR → FlowCore

Você é um desenvolvedor .NET especialista em refatoração de bibliotecas enterprise, usando o C# MCP Tool avançado com suporte a Roslyn.

## Contexto do Projeto

Nome do Projeto: SuperMediaR (será renomeado para FlowCore)  
Descrição: Biblioteca Mediator customizada com suporte a Commands, Queries, Events, Pipeline Behaviors, Caching, Authorization, Transaction, EF Core e CQRS.  
Tecnologias: .NET 8/9, C#, Entity Framework Core, Clean Architecture, CQRS  
Workspace: D:\\Programas Visuais\\SuperMediaR\\Git

## Objetivo da Sessão

Realizar uma refatoração completa do projeto para renomear SuperMediaR → FlowCore, garantindo consistência total e zero quebra de build.

## Requisitos Específicos

### Renomeações Globais

Substituir:
- SuperMediaR → FlowCore
- ISuperMediator → IFlowMediator
- SuperMediator → FlowMediator

### Assemblies e Projetos

Renomear:
- Nome dos arquivos .csproj
- AssemblyName
- RootNamespace

Garantir que todos os projetos usem:
FlowCore.*

### Namespaces

Atualizar TODOS os namespaces.

ANTES:
namespace SuperMediaR.Pipeline

DEPOIS:
namespace FlowCore.Pipeline

Aplicar para:
- Commands
- Queries
- Events
- Handlers
- Pipeline
- Behaviors
- Abstractions
- Extensions
- DependencyInjection

### Estrutura de Pastas

FlowCore/
 ├── Abstractions/
 ├── Pipeline/
 ├── Behaviors/
 ├── Mediator/
 ├── Extensions/
 └── DependencyInjection/

### Dependency Injection

ANTES:
services.AddSuperMediaR();

DEPOIS:
services.AddFlowCore();

Implementação esperada:

public static class DependencyInjection
{
    public static IServiceCollection AddFlowCore(this IServiceCollection services)
    {
        return services;
    }
}

### Interfaces e Contratos

Atualizar todas as referências internas:
- IRequest
- IRequestHandler
- INotification
- IPipelineBehavior
- Dispatcher
- Mediator

Garantir que nenhuma referência a SuperMediaR permaneça.

### Pipeline

Validar funcionamento completo dos behaviors:
- ValidationBehavior
- LoggingBehavior
- TransactionBehavior
- AuthorizationBehavior
- CachingBehavior

### Testes

- Atualizar namespaces
- Corrigir usings
- Garantir build e execução de todos os testes

### Documentação

ANTES:
using SuperMediaR;

DEPOIS:
using FlowCore;

### Limpeza

- Remover qualquer referência residual a SuperMediaR
- Validar via busca global

## Restrições

- NÃO alterar comportamento funcional
- NÃO quebrar API pública
- NÃO remover funcionalidades
- NÃO alterar contratos sem adaptação equivalente

## Convenções

- Nome base: FlowCore
- Namespaces iniciando com FlowCore
- Código alinhado com Clean Architecture

## Validações Obrigatórias

1. Build sem erros
2. Nenhuma ocorrência de "SuperMediaR"
3. Todos os testes passando
4. DI funcionando corretamente
5. Pipeline executando corretamente

## Modo de Execução

Modo: Safe
