﻿using Microsoft.TeamFoundation.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VisualStudio.GitStashExtension.Annotations;
using VisualStudio.GitStashExtension.Extensions;
using VisualStudio.GitStashExtension.GitHelpers;
using VisualStudio.GitStashExtension.Models;

namespace VisualStudio.GitStashExtension.VS.UI
{
    public class StashListSectionViewModel: INotifyPropertyChanged
    {
        private readonly ITeamExplorer _teamExplorer;
        private readonly IServiceProvider _serviceProvider;
        private readonly GitCommandExecuter _gitCommandExecuter;
        private ObservableCollection<Stash> _stashList = new ObservableCollection<Stash>();

        public ObservableCollection<Stash> Stashes
        {
            get => _stashList;
            set
            {
                _stashList = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public StashListSectionViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _teamExplorer = _serviceProvider.GetService(typeof(ITeamExplorer)) as ITeamExplorer;
            _gitCommandExecuter = new GitCommandExecuter(serviceProvider);

            UpdateStashList(string.Empty);
        }

        /// <summary>
        /// Updates stash list content based on search text.
        /// </summary>
        /// <param name="searchText">Search text.</param>
        public void UpdateStashList(string searchText)
        {
            Task.Run(() =>
            {
                if (_gitCommandExecuter.TryGetAllStashes(out var stashes, out var error))
                {
                    Stashes = stashes.Where(s => string.IsNullOrEmpty(searchText) || s.Message.Contains(searchText)).ToObservableCollection();
                }
                else
                {
                    _teamExplorer?.ShowNotification(error, NotificationType.Error, NotificationFlags.None, null, Guid.NewGuid());
                }
            });
        }

        /// <summary>
        /// Applies stash to current repository state.
        /// </summary>
        /// <param name="stashId">Stash Id.</param>
        public void ApplyStash(int stashId)
        {
            if (_gitCommandExecuter.TryApplyStash(stashId, out var errorMessage))
            {
                _teamExplorer.NavigateToPage(new Guid(TeamExplorerPageIds.GitChanges), null);
            }
            else
            {
                _teamExplorer?.ShowNotification(errorMessage, NotificationType.Error, NotificationFlags.None, null, Guid.NewGuid());
            }
        }

        /// <summary>
        /// Removes stash by id.
        /// </summary>
        /// <param name="stashId">Stash Id.</param>
        public void DeleteStash(int stashId)
        {
            if (_gitCommandExecuter.TryDeleteStash(stashId, out var errorMessage))
            {
                _teamExplorer.CurrentPage.RefreshPageAndSections();
            }
            else
            {
                _teamExplorer?.ShowNotification(errorMessage, NotificationType.Error, NotificationFlags.None, null, Guid.NewGuid());
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
