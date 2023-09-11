import {Button, Col, MenuProps, Row} from 'antd';
import React, {useContext, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {Link} from 'react-router-dom';

import {PlusOutlined} from '@ant-design/icons';

import {GcCard} from '../../components/GcCard';
import {
  GcMenuItem, GcPageContainer, GcPageContainerContent, GcPageContainerDrawers,
  GcPageContainerHeader,
} from '../../components/GcPageContainer';
import {Game} from '../../models';
import {NewPlayDrawer} from '../Plays';
import SearchGameDrawer from './components/SearchGameDrawer';
import {GamesContext} from './context';
import {GameDetailContextProvider} from './context/GameDetailProvider';

export const Games = () => {
  const { t } = useTranslation();
  const [openNewBggGame, setOpenNewBggGame] = useState(false);
  const { games, loading } = useContext(GamesContext);
  const [open, setOpen] = useState(false);
  const [game, setGame] = useState<Game | undefined>(undefined);
  const [gameId, setGameId] = useState<number | null>(null);

  useEffect(() => {
    if (gameId !== null && games.length > 0) {
      setGame(games.find((game) => game.id === gameId));
      setOpen(true);
    }
  }, [gameId, games]);

  const closeDrawer = () => {
    setOpen(false);
    setGameId(null);
  }

  const items: GcMenuItem[] = [
    {
      buttonType: 'primary',
      icon: <PlusOutlined />,
      onClick: () => { console.log("trigered games drawer"); setOpenNewBggGame(true) },
      content: t('games.new.button')
    },
    {
      buttonType: 'primary',
      icon: <PlusOutlined />,
      onClick: () => { setGame(undefined); setOpen(true) },
      content: t('play.new.button')
    }
  ];

  console.log(games.length)
  console.log(loading)

  return (
    <GcPageContainer>
      <GcPageContainerHeader
        title={t('common.games')}
        isLoading={loading}
        items={items}
      />
      <GcPageContainerContent isLoading={loading || games.length === 0}>
        <Row gutter={[10, 10]}>
          {games.map(game =>
            <Col xxl={2} xl={4} md={6} sm={12} xs={12} key={game.id}>
              <GcCard
                id={game.id}
                title={game.title}
                image={game.image}
                detailPage="games"
                state={game.state}
                showPlayLink
                setGameId={setGameId}
              />
            </Col>
          )}
        </Row>
      </GcPageContainerContent>
      <GcPageContainerDrawers>
        <GameDetailContextProvider>
          <NewPlayDrawer open={open} close={closeDrawer} game={game} key={game?.id} />
        </GameDetailContextProvider>
        <SearchGameDrawer setOpen={setOpenNewBggGame} open={openNewBggGame} />
      </GcPageContainerDrawers>
    </GcPageContainer>
  )
}

