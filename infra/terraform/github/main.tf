resource "github_repository" "repo" {
  name               = var.repo_name
  visibility         = var.repo_visibility
  has_issues         = true
  has_projects       = true
  has_wiki           = false
  delete_branch_on_merge = true
  allow_merge_commit = false
  allow_rebase_merge = false
  allow_squash_merge = true
  archive_on_destroy = false
  topics             = ["unity", "card-game", "mobile", "nuke-build", "spec-kit"]
  vulnerability_alerts = true
  auto_init          = false
  homepage_url       = null
  description        = "Unity 2022.3 LTS mobile card game with spec-driven development"
}

# Branch Protection Rules
resource "github_branch_protection" "main" {
  repository_id = github_repository.repo.node_id
  pattern       = var.default_branch

  required_status_checks {
    strict   = true
    contexts = var.required_status_checks
  }

  required_pull_request_reviews {
    dismiss_stale_reviews           = true
    require_code_owner_reviews      = true
    required_approving_review_count = var.required_approving_review_count
    require_last_push_approval      = true
  }

  enforce_admins                  = false
  require_signed_commits          = false
  required_linear_history         = true
  require_conversation_resolution = true
  allows_deletions                = false
  allows_force_pushes             = false
  lock_branch                     = false
}

# Repository Secrets
resource "github_actions_secret" "secrets" {
  for_each        = var.repository_secrets
  repository      = github_repository.repo.name
  secret_name     = each.key
  plaintext_value = each.value
}

# Repository Variables
resource "github_actions_variable" "variables" {
  for_each      = var.repository_variables
  repository    = github_repository.repo.name
  variable_name = each.key
  value         = each.value
}

# Issue Labels
resource "github_issue_label" "labels" {
  for_each    = var.issue_labels
  repository  = github_repository.repo.name
  name        = each.key
  color       = each.value.color
  description = lookup(each.value, "description", null)
}

# Branch Protection for develop (if using GitFlow)
resource "github_branch_protection" "develop" {
  count         = var.protect_develop_branch ? 1 : 0
  repository_id = github_repository.repo.node_id
  pattern       = "develop"

  required_status_checks {
    strict   = true
    contexts = var.required_status_checks
  }

  required_pull_request_reviews {
    dismiss_stale_reviews           = true
    require_code_owner_reviews      = false
    required_approving_review_count = 1
  }

  enforce_admins                  = false
  require_signed_commits          = false
  required_linear_history         = false
  require_conversation_resolution = true
  allows_deletions                = false
  allows_force_pushes             = false
}

# CODEOWNERS file (optional, managed via Terraform)
resource "github_repository_file" "codeowners" {
  count               = var.create_codeowners_file ? 1 : 0
  repository          = github_repository.repo.name
  branch              = var.default_branch
  file                = ".github/CODEOWNERS"
  content             = var.codeowners_content
  commit_message      = "Add CODEOWNERS file via Terraform"
  commit_author       = "Terraform"
  commit_email        = "terraform@example.com"
  overwrite_on_create = true
}
