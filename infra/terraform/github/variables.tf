variable "github_owner" {
  type        = string
  description = "GitHub owner (username or organization)"
}

variable "github_token" {
  type        = string
  sensitive   = true
  description = "GitHub personal access token with repo and admin:org permissions"
}

variable "repo_name" {
  type        = string
  default     = "sango-card"
  description = "Repository name"
}

variable "repo_visibility" {
  type        = string
  default     = "private"
  description = "Repository visibility (public or private)"
  validation {
    condition     = contains(["public", "private"], var.repo_visibility)
    error_message = "Repository visibility must be either 'public' or 'private'."
  }
}

variable "default_branch" {
  type        = string
  default     = "main"
  description = "Default branch name"
}

variable "protect_develop_branch" {
  type        = bool
  default     = false
  description = "Whether to protect the develop branch (for GitFlow workflows)"
}

variable "required_status_checks" {
  type        = list(string)
  default     = ["build", "test", "lint"]
  description = "List of required status checks that must pass before merging"
}

variable "required_approving_review_count" {
  type        = number
  default     = 1
  description = "Number of required approving reviews before merging"
}

variable "repository_secrets" {
  type        = map(string)
  sensitive   = true
  default     = {}
  description = "GitHub Actions secrets to create (key-value pairs)"
}

variable "repository_variables" {
  type        = map(string)
  default     = {}
  description = "GitHub Actions variables to create (key-value pairs)"
}

variable "issue_labels" {
  type = map(object({
    color       = string
    description = optional(string)
  }))
  default = {
    bug = {
      color       = "d73a4a"
      description = "Something isn't working"
    }
    feature = {
      color       = "0e8a16"
      description = "New feature or request"
    }
    rfc = {
      color       = "1d76db"
      description = "Request for comments"
    }
    infra = {
      color       = "5319e7"
      description = "Infrastructure related"
    }
    build = {
      color       = "fbca04"
      description = "Build system related"
    }
    unity = {
      color       = "000000"
      description = "Unity engine related"
    }
    gameplay = {
      color       = "eb6420"
      description = "Gameplay mechanics"
    }
    ui = {
      color       = "d4c5f9"
      description = "User interface"
    }
    p0 = {
      color       = "b60205"
      description = "Critical priority"
    }
    p1 = {
      color       = "d93f0b"
      description = "High priority"
    }
    p2 = {
      color       = "fbca04"
      description = "Medium priority"
    }
    p3 = {
      color       = "c2e0c6"
      description = "Low priority"
    }
    documentation = {
      color       = "0075ca"
      description = "Documentation improvements"
    }
    dependencies = {
      color       = "0366d6"
      description = "Dependency updates"
    }
    enhancement = {
      color       = "a2eeef"
      description = "Enhancement to existing feature"
    }
    spec-kit = {
      color       = "7057ff"
      description = "Spec-Kit related"
    }
  }
  description = "Issue labels to create"
}

variable "create_codeowners_file" {
  type        = bool
  default     = false
  description = "Whether to create a CODEOWNERS file via Terraform"
}

variable "codeowners_content" {
  type        = string
  default     = ""
  description = "Content for the CODEOWNERS file"
}
