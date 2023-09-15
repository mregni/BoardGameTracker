import {Col, Image, Row} from 'antd';
import {useContext} from 'react';
import {Trans, useTranslation} from 'react-i18next';
import {useNavigate} from 'react-router-dom';

import {DeleteOutlined, EditOutlined} from '@ant-design/icons';

import {
  GcMenuItem, GcPageContainer, GcPageContent, GcPageHeader,
} from '../../../components/GcPageContainer';
import {GcStateRibbon} from '../../../components/GcStateRibbon';
import {useModals} from '../../../utils';
import {GameDetailContext, GamesContext} from '../context';
import {GameHeader} from './GameHeader';
import {GamePlaysTable} from './GamePlaysTable';
import {GameStatistics} from './GameStatistics';

const GameDetailOverview = () => {
  const { loading, deleteGame, game } = useContext(GameDetailContext);
  const { loadGames } = useContext(GamesContext);
  const navigate = useNavigate();
  const { deleteModal } = useModals();
  const { t } = useTranslation();

  if (game === null) {
    return (<></>);
  }

  const showDeleteModal = () => {
    deleteModal(
      t('game.delete.title', { title: game.title }),
      <Trans
        i18nKey="game.delete.description"
        values={{ title: game.title }}
        components={{ strong: <strong />, newline: <br /> }} />,
      localDeleteGame
    );
  }

  const localDeleteGame = async () => {
    await deleteGame();
    await loadGames();
    navigate('/games');
  }

  const items: GcMenuItem[] = [
    {
      buttonType: 'primary',
      icon: <EditOutlined />,
      onClick: () => console.log("edit"),
      content: t('common.edit')
    },
    {
      buttonType: 'primary',
      icon: <DeleteOutlined />,
      onClick: () => showDeleteModal(),
      content: t('common.delete'),
    }
  ];

  return (
    <GcPageContainer>
      <GcPageHeader
        hasBack
        backNavigation='/games'
        isLoading={loading}
        title={game.title}
        items={items}
      />
      <GcPageContent isLoading={loading} hasData={game !== null}>
        <Row gutter={[16, 16]}>
          <Col xxl={3} xl={4} md={5} xs={12}>
            <GcStateRibbon state={game.state}>
              <Image
                preview={false}
                width={'100%'}
                src={`/${game.image}`}
                className="image"
              />
            </GcStateRibbon>
          </Col>
          <Col xxl={21} xl={20} md={19} sm={12} xs={12}>
            <GameHeader />
          </Col>
          <Col xs={24}>
            <GameStatistics />
          </Col>
          <Col xs={24}>
            <GamePlaysTable />
          </Col>
        </Row>
      </GcPageContent>
    </GcPageContainer>
  )
}

export default GameDetailOverview
