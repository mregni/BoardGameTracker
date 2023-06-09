import {App} from 'antd';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';

import {Game, SearchResult, SearchResultType} from '../../models';
import {addGame} from '../../services/BggService';
import {
  createErrorNotification, createInfoNotification, createSuccessNotification,
  createWarningNotification,
} from '../../utils';

interface Props {
  search: (bggId: string, state: string) => Promise<SearchResultType>;
  searching: boolean;
}

export default function useSearchGame(): Props {
  const [searching, setSearching] = useState(false);
  const { t } = useTranslation();
  const { notification } = App.useApp();

  const search = async (bggId: string, state: string): Promise<SearchResultType> => {
    setSearching(true);
    let searchResult = SearchResultType.Failed;
    await addGame(bggId, state)
      .then((result: SearchResult<Game>) => {
        if (result.result === SearchResultType.Found) {
          createSuccessNotification(
            notification,
            t('games.new.success.message'),
            t('games.new.success.description', { name: result.model?.title })
          )
        } else if (result.result === SearchResultType.NotFound) {
          createWarningNotification(
            notification,
            t('games.new.notfound.message'),
            t('games.new.notfound.description', { id: bggId })
          );
        } else if (result.result === SearchResultType.Duplicate) {
          createInfoNotification(
            notification,
            t('games.new.duplicate.message'),
            t('games.new.duplicate.description', { name: result.model?.title })
          );
        }
        searchResult = result.result;
      }).catch(() => {
        createErrorNotification(
          notification,
          t('games.new.failed.message'),
          t('games.new.failed.description', { id: bggId })
        )
      }).finally(() => {
        setSearching(false);
      })

    return searchResult;
  }

  return { search, searching }
}