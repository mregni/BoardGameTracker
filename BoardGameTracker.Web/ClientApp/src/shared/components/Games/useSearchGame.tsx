import {App} from 'antd';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';

import {GameSearchResult, SearchResult} from '../../models';
import {searchGame} from '../../services/BggService';
import {
  createErrorNotification, createInfoNotification, createSuccessNotification,
  createWarningNotification,
} from '../../utils';

interface Props {
  search: (bggId: string) => Promise<SearchResult>;
  searching: boolean;
}

export default function useSearchGame(): Props {
  const [searching, setSearching] = useState(false);
  const { t } = useTranslation();
  const { notification } = App.useApp();

  const search = async (bggId: string): Promise<SearchResult> => {
    let searchResult = SearchResult.Failed;
    await searchGame(bggId)
      .then((result: GameSearchResult) => {
        console.log(result)
        if(result.result === SearchResult.Found){
          createSuccessNotification(
            notification,
            t('games.new.success.message'),
            t('games.new.success.description', { name: result.game?.title })
          )
        } else if(result.result === SearchResult.NotFound){
          createWarningNotification(
            notification,
            t('games.new.notfound.message'),
            t('games.new.notfound.description', { id: bggId })
          );
        } else if(result.result === SearchResult.Duplicate){
          createInfoNotification(
            notification,
            t('games.new.duplicate.message'),
            t('games.new.duplicate.description', { name: result.game?.title })
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