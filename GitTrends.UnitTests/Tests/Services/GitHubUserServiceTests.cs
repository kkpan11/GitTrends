﻿using System;
using System.Threading.Tasks;
using System.Web;
using GitTrends.Shared;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GitTrends.UnitTests
{
	class GitHubUserServiceTests : BaseTest
	{
		const string _tokenType = "Bearer";

		readonly string _scope = GitHubConstants.OAuthScope;
		readonly string _token = Guid.NewGuid().ToString();

		[Test]
		public async Task AliasTest()
		{
			//Arrange
			string alias_Initial, aliasChangedResult, alias_Final;

			bool didAliasChangedFire = false;
			var aliasChangedTCS = new TaskCompletionSource<string>();

			GitHubUserService.AliasChanged += HandleAliasChanged;

			var gitHubUserService = ServiceCollection.ServiceProvider.GetRequiredService<GitHubUserService>();
			alias_Initial = gitHubUserService.Alias;

			//Act
			gitHubUserService.Alias = GitHubConstants.GitTrendsRepoOwner;
			aliasChangedResult = await aliasChangedTCS.Task.ConfigureAwait(false);
			alias_Final = gitHubUserService.Alias;

			//Assert
			Assert.IsTrue(didAliasChangedFire);
			Assert.AreEqual(string.Empty, alias_Initial);
			Assert.AreEqual(GitHubConstants.GitTrendsRepoOwner, alias_Final);
			Assert.AreEqual(alias_Final, aliasChangedResult);


			void HandleAliasChanged(object? sender, string e)
			{
				GitHubUserService.AliasChanged -= HandleAliasChanged;

				didAliasChangedFire = true;
				aliasChangedTCS.SetResult(e);
			}
		}

		[Test]
		public async Task NameTest()
		{
			//Arrange
			const string expectedName = "Brandon Minnick";

			string name_Initial, nameChangedResult, name_Final;

			bool didNameChangedFire = false;
			var nameChangedTCS = new TaskCompletionSource<string>();

			GitHubUserService.NameChanged += HandleNameChanged;

			var gitHubUserService = ServiceCollection.ServiceProvider.GetRequiredService<GitHubUserService>();
			name_Initial = gitHubUserService.Name;

			//Act
			gitHubUserService.Name = expectedName;
			nameChangedResult = await nameChangedTCS.Task.ConfigureAwait(false);
			name_Final = gitHubUserService.Name;

			//Assert
			Assert.IsTrue(didNameChangedFire);
			Assert.AreEqual(string.Empty, name_Initial);
			Assert.AreEqual(expectedName, name_Final);
			Assert.AreEqual(name_Final, nameChangedResult);


			void HandleNameChanged(object? sender, string e)
			{
				GitHubUserService.NameChanged -= HandleNameChanged;

				didNameChangedFire = true;
				nameChangedTCS.SetResult(e);
			}
		}

		[Test]
		public async Task AvatarUrlTest()
		{
			//Arrange
			string avatarUrl_Initial, avatarUrlChangedResult, avatarUrl_Final;

			bool didAvatarUrlChangedFire = false;
			var avatarUrlChangedTCS = new TaskCompletionSource<string>();

			GitHubUserService.AvatarUrlChanged += HandleAvatarUrlChanged;

			var gitHubUserService = ServiceCollection.ServiceProvider.GetRequiredService<GitHubUserService>();
			avatarUrl_Initial = gitHubUserService.AvatarUrl;

			//Act
			gitHubUserService.AvatarUrl = AuthenticatedGitHubUserAvatarUrl;
			avatarUrlChangedResult = await avatarUrlChangedTCS.Task.ConfigureAwait(false);
			avatarUrl_Final = gitHubUserService.AvatarUrl;

			//Assert
			Assert.IsTrue(didAvatarUrlChangedFire);
			Assert.AreEqual(string.Empty, avatarUrl_Initial);
			Assert.AreEqual(AuthenticatedGitHubUserAvatarUrl, avatarUrl_Final);
			Assert.AreEqual(avatarUrl_Final, avatarUrlChangedResult);


			void HandleAvatarUrlChanged(object? sender, string e)
			{
				GitHubUserService.AvatarUrlChanged -= HandleAvatarUrlChanged;

				didAvatarUrlChangedFire = true;
				avatarUrlChangedTCS.SetResult(e);
			}
		}

		[Test]
		public async Task SaveGitHubTokenTest_ValidToken()
		{
			//Arrange
			var gitHubToken = new GitHubToken(_token, _scope, _tokenType);
			var gitHubUserService = ServiceCollection.ServiceProvider.GetRequiredService<GitHubUserService>();

			//Act
			await gitHubUserService.SaveGitHubToken(gitHubToken).ConfigureAwait(false);
			var retrievedToken = await gitHubUserService.GetGitHubToken().ConfigureAwait(false);

			//Assert
			Assert.AreEqual(_token, retrievedToken.AccessToken);
			Assert.AreEqual(_scope, retrievedToken.Scope);
			Assert.AreEqual(_tokenType, retrievedToken.TokenType);

			Assert.AreEqual(gitHubToken.AccessToken, retrievedToken.AccessToken);
			Assert.AreEqual(gitHubToken.Scope, retrievedToken.Scope);
			Assert.AreEqual(gitHubToken.TokenType, retrievedToken.TokenType);
		}

		[Test]
		public async Task SaveGitHubTokenTest_InvalidScopes()
		{
			//Arrange
			var scopes_MissingOrg = HttpUtility.UrlEncode("public_repo read:user");

			var gitHubToken = new GitHubToken(_token, scopes_MissingOrg, _tokenType);
			var gitHubUserService = ServiceCollection.ServiceProvider.GetRequiredService<GitHubUserService>();

			//Act
			await gitHubUserService.SaveGitHubToken(gitHubToken).ConfigureAwait(false);
			var retrievedToken = await gitHubUserService.GetGitHubToken().ConfigureAwait(false);

			//Assert
			Assert.AreEqual(GitHubToken.Empty.AccessToken, retrievedToken.AccessToken);
			Assert.AreEqual(GitHubToken.Empty.Scope, retrievedToken.Scope);
			Assert.AreEqual(GitHubToken.Empty.TokenType, retrievedToken.TokenType);
		}

		[Test]
		public async Task InvalidateTokenTest()
		{
			//Arrange
			GitHubToken? token_BeforeInvalidation, token_AfterInvalidation;
			var gitHubUserService = ServiceCollection.ServiceProvider.GetRequiredService<GitHubUserService>();

			//Act
			await SaveGitHubTokenTest_ValidToken().ConfigureAwait(false);

			token_BeforeInvalidation = await gitHubUserService.GetGitHubToken().ConfigureAwait(false);

			gitHubUserService.InvalidateToken();

			token_AfterInvalidation = await gitHubUserService.GetGitHubToken().ConfigureAwait(false);

			//Assert
			Assert.AreEqual(_token, token_BeforeInvalidation.AccessToken);
			Assert.AreEqual(_scope, token_BeforeInvalidation.Scope);
			Assert.AreEqual(_tokenType, token_BeforeInvalidation.TokenType);

			Assert.AreEqual(GitHubToken.Empty.AccessToken, token_AfterInvalidation.AccessToken);
			Assert.AreEqual(GitHubToken.Empty.Scope, token_AfterInvalidation.Scope);
			Assert.AreEqual(GitHubToken.Empty.TokenType, token_AfterInvalidation.TokenType);
		}

		[Test]
		public async Task ShouldIncludeOrganizationsTest()
		{
			//Arrange
			bool shouldIncludeOrganizations_Initial, shouldIncludeOrganizations_Final;
			var shouldIncludeOrganizationsChangedTCS = new TaskCompletionSource<bool>();
			var gitHubUserService = ServiceCollection.ServiceProvider.GetRequiredService<GitHubUserService>();

			GitHubUserService.ShouldIncludeOrganizationsChanged += HandleShouldIncludeOrganizationsChanged;

			//Act
			shouldIncludeOrganizations_Initial = gitHubUserService.ShouldIncludeOrganizations;

			gitHubUserService.ShouldIncludeOrganizations = !gitHubUserService.ShouldIncludeOrganizations;
			var shouldIncludeOrganizationsChangedResult = await shouldIncludeOrganizationsChangedTCS.Task.ConfigureAwait(false);

			shouldIncludeOrganizations_Final = gitHubUserService.ShouldIncludeOrganizations;

			//Assert
			Assert.IsFalse(shouldIncludeOrganizations_Initial);
			Assert.IsTrue(shouldIncludeOrganizationsChangedResult);
			Assert.IsTrue(shouldIncludeOrganizations_Final);

			void HandleShouldIncludeOrganizationsChanged(object? sender, bool e)
			{
				GitHubUserService.ShouldIncludeOrganizationsChanged -= HandleShouldIncludeOrganizationsChanged;
				shouldIncludeOrganizationsChangedTCS.SetResult(e);
			}
		}
	}
}