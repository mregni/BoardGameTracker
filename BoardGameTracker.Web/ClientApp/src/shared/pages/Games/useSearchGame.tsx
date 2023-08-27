import {App} from 'antd';
import {useState} from 'react';
import {useTranslation} from 'react-i18next';

import {BggSearch, Game, SearchResult, SearchResultType} from '../../models';
import {addGame} from '../../services/BggService';
import {
  createErrorNotification, createInfoNotification, createSuccessNotification,
  createWarningNotification,
} from '../../utils';

interface Props {
  search: (model: BggSearch) => Promise<SearchResultType>;
  searching: boolean;
}

export default function useSearchGame(): Props {
  const [searching, setSearching] = useState(false);
  const { t } = useTranslation();
  const { notification } = App.useApp();

  const search = async (model: BggSearch): Promise<SearchResultType> => {
    setSearching(true);
    let searchResult = SearchResultType.Failed;
    await addGame(model)
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
            t('games.new.notfound.description', { id: model.bggId })
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
          t('games.new.failed.description', { id: model.bggId })
        )
      }).finally(() => {
        setSearching(false);
      })

    return searchResult;
  }

  return { search, searching }
}