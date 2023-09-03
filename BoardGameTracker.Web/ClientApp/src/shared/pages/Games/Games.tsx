import {Button, Col, FloatButton, Row} from 'antd';
import React, {useContext, useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';

import {GlobalOutlined, PlusOutlined} from '@ant-design/icons';

import {GcCard} from '../../components/GcCard';
import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../components/GcPageContainer';
import {Game} from '../../models';
import {GameDetailContextProvider} from '../GameDetail/context/GameDetailProvider';
import {NewPlayDrawer} from '../Plays';
import SearchGameDrawer from './components/SearchGameDrawer';
import {GamesContext} from './context';

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

  return (
    <GcPageContainer>
      <GcPageContainerHeader
        title={t('common.games')}
        isLoading={loading || games.length === 0}>
        <Button
          icon={<PlusOutlined />}
          type="primary"
          onClick={() => setOpenNewBggGame(true)}
        >
          {t('common.add-new')}
        </Button>
      </GcPageContainerHeader>
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
        <GameDetailContextProvider>
          <NewPlayDrawer open={open} close={closeDrawer} game={game} />
        </GameDetailContextProvider>
        <SearchGameDrawer setOpen={setOpenNewBggGame} open={openNewBggGame} />
        <FloatButton.Group
          trigger="hover"
          style={{ right: 48 }}
          icon={<PlusOutlined />}
          type="primary"
        >
          <FloatButton tooltip={t('games.manual')} icon={<PlusOutlined />} />
          <FloatButton
            tooltip={t('games.bgg')}
            icon={<GlobalOutlined />}
            onClick={() => setOpenNewBggGame(true)} />
        </FloatButton.Group>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}

