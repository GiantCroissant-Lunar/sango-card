# Terraform Cloud Backend Configuration
# Uncomment and configure for Terraform Cloud

terraform {
  cloud {
    organization = "your-terraform-cloud-org"

    workspaces {
      name = "sango-card-github"
    }
  }
}

# Alternative: Local backend (default if cloud block is commented)
# terraform {
#   backend "local" {
#     path = "terraform.tfstate"
#   }
# }

# Alternative: S3 backend
# terraform {
#   backend "s3" {
#     bucket         = "your-terraform-state-bucket"
#     key            = "sango-card/github/terraform.tfstate"
#     region         = "us-east-1"
#     encrypt        = true
#     dynamodb_table = "terraform-state-lock"
#   }
# }
