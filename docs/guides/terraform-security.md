---
doc_id: DOC-2025-00131
title: Terraform Security
doc_type: guide
status: active
canonical: false
created: 2025-10-17
tags: [terraform-security]
summary: >
  (Add summary here)
source:
  author: system
---
---
doc_id: DOC-2025-00081
title: Terraform Security Guide
doc_type: guide
status: active
canonical: true
created: '2025-10-17'
tags:

- terraform
- security
- sops
- encryption
summary: Security best practices for managing encrypted Terraform secrets.

---

# Security Guide - SOPS & Terraform

Security best practices for managing encrypted secrets in the sango-card infrastructure.

## Security Layers

### 1. Encryption at Rest (SOPS + age)

**What's Encrypted:**

- Unity licenses and credentials
- GitHub tokens and API keys
- Android/iOS signing certificates
- All sensitive repository secrets

**How It Works:**

```
Plaintext JSON → age encryption → SOPS metadata → Encrypted file
     ↓                                                    ↓
  Gitignored                                      Committed to git
```

**Age Encryption:**

- Modern, secure encryption (ChaCha20-Poly1305)
- Public key encryption (like SSH keys)
- Small encrypted files (~2x original size)
- Fast encryption/decryption

### 2. Access Control (age keys)

**Private Key Security:**

- Stored locally only (`secrets/age.key`)
- Never committed to git (`.gitignore` protected)
- Needed only for decryption
- Should be backed up securely

**Public Key:**

- Safe to share and commit (in `.sops.yaml`)
- Used only for encryption
- Can be regenerated anytime

### 3. Git Protection (Pre-commit Hooks)

**Automated Checks:**

1. **Plaintext secrets detection** - Blocks commit if unencrypted secrets detected
2. **Age key detection** - Prevents committing private keys
3. **SOPS validation** - Ensures `.sops.yaml` has real key, not placeholder
4. **Secret scanning** - Gitleaks and detect-secrets integration

**Manual Override (NOT RECOMMENDED):**

```powershell
# NEVER do this unless you know exactly what you're doing
git commit --no-verify -m "message"
```

### 4. CI/CD Security (GitHub Actions)

**Secret Storage:**

- Age private key stored in GitHub Secrets (`SOPS_AGE_KEY`)
- Encrypted at rest by GitHub
- Only available to workflows
- Redacted in logs

**Runtime Security:**

- Secrets decrypted in memory only
- Files written to ephemeral runners
- Automatic cleanup after job completes
- No secrets in build artifacts

## Security Best Practices

### ✅ DO

**Key Management:**

- ✅ Generate unique age keys per team member (optional)
- ✅ Back up age key to secure password manager
- ✅ Rotate age keys annually
- ✅ Use strong passphrases if encrypting age key

**Secret Management:**

- ✅ Always encrypt secrets before committing
- ✅ Use descriptive names for secrets (non-sensitive)
- ✅ Review encrypted diffs before committing
- ✅ Audit who has access to `SOPS_AGE_KEY` GitHub secret

**Operational:**

- ✅ Run pre-commit hooks on every commit
- ✅ Use `.\Encrypt-Secrets.ps1 -Force` when updating
- ✅ Test decryption locally before pushing
- ✅ Monitor GitHub Actions logs for suspicious activity

**Team Collaboration:**

- ✅ Share only public keys (in `.sops.yaml`)
- ✅ Use separate age keys per environment (dev/staging/prod)
- ✅ Document key rotation procedures
- ✅ Revoke access by removing public keys and re-encrypting

### ❌ DON'T

**Never Commit:**

- ❌ `secrets/age.key` (private key)
- ❌ `secrets/*.json` (plaintext secrets)
- ❌ `github/terraform.tfvars` (generated variables)
- ❌ `.terraform.tfstate` (state files)

**Never Share:**

- ❌ Private age keys via Slack/email/chat
- ❌ Screenshots showing plaintext secrets
- ❌ Terraform plan outputs with sensitive data
- ❌ GitHub `SOPS_AGE_KEY` secret value

**Never Do:**

- ❌ Skip pre-commit hooks (`--no-verify`)
- ❌ Commit plaintext secrets "temporarily"
- ❌ Store age key in code repository
- ❌ Use the same key for multiple environments

## Threat Model

### What SOPS Protects Against

✅ **Accidental commits** - Plaintext secrets in git history  
✅ **Repository access** - Attackers with repo access can't read secrets  
✅ **Public repository exposure** - Safe to open-source (with secrets encrypted)  
✅ **CI/CD compromise** - Even with repo access, can't decrypt without key  
✅ **Developer laptop theft** - Encrypted files are useless without age key  

### What SOPS Does NOT Protect Against

❌ **Compromised age key** - If attacker gets key, can decrypt all secrets  
❌ **Compromised CI/CD** - If `SOPS_AGE_KEY` GitHub secret is stolen  
❌ **Memory dumps** - Secrets in memory during Terraform runs  
❌ **Terraform state** - State may contain secrets in plaintext  
❌ **Logging** - Accidental logging of decrypted secrets  

### Additional Protections

**For Terraform State:**

- Use Terraform Cloud (encrypted at rest)
- Or use S3 with encryption enabled
- Never commit `.tfstate` files
- Limit access to state storage

**For CI/CD:**

- Restrict who can modify workflows
- Use branch protection rules
- Require PR reviews for infrastructure changes
- Enable GitHub Actions audit log

**For Logging:**

- Redact sensitive values in logs
- Disable verbose logging in production
- Review logs for accidental secret exposure
- Use `sensitive = true` in Terraform variables

## Key Rotation

### When to Rotate

- 🔄 Annually (proactive)
- 🔄 When team member leaves
- 🔄 If key is compromised
- 🔄 If key is accidentally exposed

### Rotation Process

1. **Generate new key:**

   ```powershell
   cd infra\terraform\scripts
   .\New-AgeKeyPair.ps1 -OutFile ..\secrets\age-new.key
   ```

2. **Add new public key to `.sops.yaml`** (keep old):

   ```yaml
   creation_rules:
     - age:
         - "age1old_key_here"      # Keep old key
         - "age1new_key_here"      # Add new key
   ```

3. **Re-encrypt with both keys:**

   ```powershell
   .\Encrypt-Secrets.ps1 -Force
   git add ..\secrets\*.encrypted ..\.sops.yaml
   git commit -m "chore(security): rotate age key"
   git push
   ```

4. **Update GitHub secret:**
   - Repository Settings → Secrets → `SOPS_AGE_KEY`
   - Update with contents of `secrets/age-new.key`

5. **Verify CI/CD works** with new key

6. **Remove old public key from `.sops.yaml`:**

   ```yaml
   creation_rules:
     - age:
         - "age1new_key_here"      # Only new key
   ```

7. **Re-encrypt to remove old key:**

   ```powershell
   .\Encrypt-Secrets.ps1 -Force
   git add ..\secrets\*.encrypted ..\.sops.yaml
   git commit -m "chore(security): remove old age key"
   git push
   ```

8. **Securely delete old key:**

   ```powershell
   Remove-Item ..\secrets\age.key -Force
   Rename-Item ..\secrets\age-new.key age.key
   ```

## Incident Response

### If Age Key is Compromised

1. **Immediate:**
   - Generate new age key pair
   - Update `.sops.yaml` with new key only
   - Re-encrypt all secrets
   - Push to git immediately

2. **Within 1 hour:**
   - Rotate all secrets (GitHub tokens, Unity licenses, etc.)
   - Update GitHub `SOPS_AGE_KEY` secret
   - Notify team members
   - Review git history for suspicious commits

3. **Within 24 hours:**
   - Audit who had access
   - Review CI/CD logs
   - Document incident
   - Update security procedures

### If Plaintext Secret is Committed

1. **Immediate:**
   - DO NOT just delete the file and commit
   - Secret is now in git history permanently
   - Rotate the compromised secret immediately

2. **Clean git history** (if repository is private):

   ```powershell
   # Use BFG Repo Cleaner or git filter-repo
   # WARNING: This rewrites history
   git filter-repo --path secrets/terraform-secrets.json --invert-paths
   git push origin --force --all
   ```

3. **If repository is public:**
   - Assume secret is compromised
   - Rotate immediately
   - Consider repository as tainted
   - May need to create new repository

## Compliance

### Audit Trail

**What's Tracked:**

- ✅ When secrets were updated (git commits)
- ✅ Who encrypted secrets (git author)
- ✅ When keys were rotated (git history)
- ✅ CI/CD deployments (GitHub Actions logs)

**What's NOT Tracked:**

- ❌ Who decrypted secrets locally
- ❌ How many times secrets were accessed
- ❌ Who viewed GitHub `SOPS_AGE_KEY` secret

### GDPR / Data Privacy

**Personal Data:**

- Unity email addresses may be personal data
- Age keys are not personal data (cryptographic keys)

**Data Retention:**

- Encrypted secrets stored in git indefinitely
- Plaintext secrets never stored in git
- CI/CD logs retained per GitHub settings (default: 90 days)

**Right to be Forgotten:**

- Remove user's public key from `.sops.yaml`
- Re-encrypt secrets to revoke access
- No need to delete git history (only encrypted data)

## Security Checklist

Before first deployment:

- [ ] Age key generated and backed up securely
- [ ] `.sops.yaml` updated with real public key (not placeholder)
- [ ] `secrets/terraform-secrets.json` created with real secrets
- [ ] Secrets encrypted with `.\Encrypt-Secrets.ps1`
- [ ] `.gitignore` protecting plaintext files
- [ ] Pre-commit hooks installed (`pre-commit install`)
- [ ] GitHub `SOPS_AGE_KEY` secret configured
- [ ] CI/CD workflow tested successfully
- [ ] Team members trained on SOPS workflow
- [ ] Incident response plan documented

Periodic reviews (quarterly):

- [ ] Review who has access to `SOPS_AGE_KEY` GitHub secret
- [ ] Audit encrypted secrets for stale/unused entries
- [ ] Test key rotation procedure
- [ ] Review pre-commit hook effectiveness
- [ ] Check for secrets in git history (gitleaks scan)
- [ ] Verify backup of age key exists
- [ ] Update security documentation

## Resources

- [SOPS Security Best Practices](https://github.com/mozilla/sops#security-considerations)
- [age Encryption Tool](https://github.com/FiloSottile/age#design)
- [GitHub Secrets Security](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)

## Contact

For security issues:

- **Do NOT** open public GitHub issues
- Contact repository maintainers directly
- Use encrypted communication if possible
