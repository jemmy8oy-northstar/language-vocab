# .NET Repo Template

A GitHub repository template for full-stack .NET + Node.js projects with CI, Docker builds, GitOps, and Claude Code integration pre-configured.

## Creating a New Project From This Template

Run the following command to scaffold a new project:

```
dotnet new web-template -n YourProjectName --appName your-project-name --no-includeWorkflows --force
```

- `--no-includeWorkflows` skips the workflow files so the ones already in this repo are preserved as-is
- `--force` allows the template to overwrite existing files — this README will be replaced, which is expected

After scaffolding, push the generated code to a feature branch and raise a PR into `dev`. Do not raise PRs directly into `main` — the branch protection rules will block it.

## Workflow Files

This template includes four pre-configured GitHub Actions workflows that are intentionally excluded from the scaffold command above.

| File | Purpose |
|------|---------|
| `ci.yml` | Builds backend (.NET) and frontend (Node.js) on every pull request |
| `check-source-branch.yml` | Enforces that PRs into `main` must come from `dev` |
| `docker-build-push.yml` | Builds and pushes ARM64 Docker images to Oracle Container Registry (OCIR), then auto-bumps the Helm chart version via GitOps |
| `claude.yml` | Enables `@claude` mentions in issues and PRs to trigger Claude Code |

## Branch Protection Rules

Apply the following ruleset in **Settings → Rules → Rulesets → New ruleset**:

```json
{
  "name": "Protected Branches",
  "target": "branch",
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "exclude": [],
      "include": [
        "refs/heads/main",
        "refs/heads/dev"
      ]
    }
  },
  "rules": [
    {
      "type": "deletion"
    },
    {
      "type": "non_fast_forward"
    },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 0,
        "dismiss_stale_reviews_on_push": false,
        "required_reviewers": [],
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_review_thread_resolution": false,
        "allowed_merge_methods": [
          "rebase",
          "merge"
        ]
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": false,
        "do_not_enforce_on_create": false,
        "required_status_checks": [
          {
            "context": "verify-branch"
          }
        ]
      }
    }
  ],
  "bypass_actors": [
    {
      "actor_id": 5,
      "actor_type": "RepositoryRole",
      "bypass_mode": "always"
    },
    {
      "actor_id": 4070856,
      "actor_type": "Integration",
      "bypass_mode": "always"
    }
  ]
}
```

This enforces:
- `main` and `dev` cannot be deleted or force-pushed
- All changes to `main` must go through a PR from `dev` (enforced by `check-source-branch.yml`)
- PRs must pass the `verify-branch` status check before merging

## Required Secrets and Variables

For the Docker build workflow to function, configure the following in **Settings → Secrets and variables**:

| Type | Name | Value |
|------|------|-------|
| Variable | `OCIR_REGISTRY` | e.g. `lhr.ocir.io` |
| Variable | `OCIR_NAMESPACE` | Your OCI tenancy namespace |
| Secret | `OCIR_USERNAME` | e.g. `tenancy/oracleidentitycloudservice/you@email.com` |
| Secret | `OCIR_AUTH_TOKEN` | OCI auth token (not your account password) |
| Secret | `CLAUDE_CODE_OAUTH_TOKEN` | OAuth token for Claude Code GitHub Actions |

The `GIT_OPS_APP_ID` variable and `GITOPS_APP_PRIVATE_KEY` secret are pulled from org-level settings and do not need to be set per-repo.
