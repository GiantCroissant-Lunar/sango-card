output "repository_name" {
  value       = github_repository.repo.name
  description = "Repository name"
}

output "repository_full_name" {
  value       = github_repository.repo.full_name
  description = "Full name of the repository (owner/repo)"
}

output "repository_html_url" {
  value       = github_repository.repo.html_url
  description = "URL to the repository on GitHub"
}

output "repository_ssh_clone_url" {
  value       = github_repository.repo.ssh_clone_url
  description = "SSH clone URL"
}

output "repository_http_clone_url" {
  value       = github_repository.repo.http_clone_url
  description = "HTTP clone URL"
}

output "repository_node_id" {
  value       = github_repository.repo.node_id
  description = "Node ID of the repository"
}

output "protected_branches" {
  value = concat(
    [github_branch_protection.main.pattern],
    var.protect_develop_branch ? [github_branch_protection.develop[0].pattern] : []
  )
  description = "List of protected branch patterns"
}

output "issue_labels" {
  value = {
    for label in github_issue_label.labels : label.name => {
      color       = label.color
      description = label.description
    }
  }
  description = "Map of created issue labels"
}
