import {Col, Row} from 'antd';
import {useContext, useEffect, useState} from 'react';

import {GcCard} from '../../components/GcCard';
import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../components/GcPageContainer';
import {Game} from '../../models';
import {GameDetailContextProvider} from '../GameDetail/context/GameDetailProvider';
import {NewPlayDrawer} from '../Plays';
import {GamesContext} from './context';

const GameList = () => {
  const { games, loading } = useContext(GamesContext);
  const [open, setOpen] = useState(false);
  const [game, setGame] = useState<Game | undefined>(undefined);
  const [gameId, setGameId] = useState<number | null>(null);

  useEffect(() => {
    if (gameId !== null && games.length > 0) {
      setGame(games.find((game) => game.id === gameId));
      setOpen(true);
    }
  }, [gameId, games])


  return (
    <GcPageContainer>
      <GcPageContainerHeader
        title='Games'
        isLoading={loading || games.length === 0}>
        Add, filter, dfg
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
          <NewPlayDrawer open={open} setOpen={setOpen} game={game} />
        </GameDetailContextProvider>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}

export default GameList