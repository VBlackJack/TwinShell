# Guide de Contribution - TwinShell

Merci de votre interet pour contribuer a TwinShell !

---

## Comment Contribuer

### 1. Signaler un Bug

Avant de creer une issue :
- Verifiez que le bug n'a pas deja ete signale
- Testez avec la derniere version de TwinShell

Pour signaler un bug :
1. Allez sur [GitHub Issues](https://github.com/VBlackJack/TwinShell/issues)
2. Cliquez sur "New Issue"
3. Utilisez le template "Bug Report"
4. Incluez :
   - Version de TwinShell
   - Version de Windows
   - Etapes pour reproduire
   - Comportement attendu vs observe
   - Screenshots si pertinent

### 2. Proposer une Fonctionnalite

1. Ouvrez une [Discussion](https://github.com/VBlackJack/TwinShell/discussions)
2. Decrivez votre idee en detail
3. Attendez les retours de la communaute
4. Si approuve, creez une issue "Feature Request"

### 3. Soumettre du Code

#### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 ou VS Code
- Git

#### Workflow

```bash
# 1. Fork le projet sur GitHub

# 2. Clonez votre fork
git clone https://github.com/VOTRE-USERNAME/TwinShell.git
cd TwinShell

# 3. Creez une branche
git checkout -b feature/ma-fonctionnalite

# 4. Faites vos modifications

# 5. Lancez les tests
dotnet test

# 6. Commitez
git add .
git commit -m "feat: description de la fonctionnalite"

# 7. Pushez
git push origin feature/ma-fonctionnalite

# 8. Creez une Pull Request sur GitHub
```

---

## Standards de Code

### Architecture

- **MVVM** : Model-View-ViewModel avec CommunityToolkit.Mvvm
- **Services** : Injection de dependances via constructeur
- **Async** : Utilisez async/await pour les operations I/O

### Conventions de Nommage

| Element | Convention | Exemple |
|---------|------------|---------|
| Classes | PascalCase | `SearchService` |
| Methodes | PascalCase | `ExecuteSearch()` |
| Variables privees | _camelCase | `_searchText` |
| Variables locales | camelCase | `searchResult` |
| Constantes | UPPER_CASE | `MAX_RESULTS` |
| Interfaces | IPascalCase | `ISearchService` |

### Documentation

- XML comments sur toutes les APIs publiques
- Commentaires en anglais dans le code
- Documentation utilisateur en francais

```csharp
/// <summary>
/// Executes a search query against the action repository.
/// </summary>
/// <param name="query">The search query string.</param>
/// <returns>A collection of matching actions.</returns>
public async Task<IEnumerable<Action>> SearchAsync(string query)
{
    // Implementation
}
```

### Tests

- Couverture minimale : 80%
- Framework : xUnit + FluentAssertions
- Nommage : `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public void Search_WithEmptyQuery_ReturnsAllActions()
{
    // Arrange
    var service = new SearchService();

    // Act
    var results = service.Search("");

    // Assert
    results.Should().NotBeEmpty();
}
```

---

## Structure des Commits

Utilisez les [Conventional Commits](https://www.conventionalcommits.org/) :

| Type | Description |
|------|-------------|
| `feat` | Nouvelle fonctionnalite |
| `fix` | Correction de bug |
| `docs` | Documentation |
| `style` | Formatage (pas de changement de code) |
| `refactor` | Refactoring |
| `test` | Ajout/modification de tests |
| `chore` | Maintenance (build, deps, etc.) |

Exemples :
```
feat: add fuzzy search capability
fix: resolve memory leak in SearchService
docs: update installation guide
refactor: extract validation logic to separate service
```

---

## Pull Requests

### Checklist

Avant de soumettre votre PR, verifiez :

- [ ] Le code compile sans erreurs
- [ ] Tous les tests passent (`dotnet test`)
- [ ] Le code suit les conventions de style
- [ ] La documentation est mise a jour si necessaire
- [ ] Les commits suivent la convention
- [ ] La PR a une description claire

### Template de PR

```markdown
## Description

Decrivez vos changements ici.

## Type de changement

- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Tests

Decrivez les tests effectues.

## Checklist

- [ ] Mon code suit les standards du projet
- [ ] J'ai effectue une auto-review
- [ ] J'ai commente le code si necessaire
- [ ] J'ai mis a jour la documentation
- [ ] Mes changements ne generent pas de warnings
- [ ] J'ai ajoute des tests
- [ ] Tous les tests passent
```

---

## Questions ?

- [GitHub Discussions](https://github.com/VBlackJack/TwinShell/discussions)
- [Issues](https://github.com/VBlackJack/TwinShell/issues)

---

*Derniere mise a jour : 2025-11-28*
