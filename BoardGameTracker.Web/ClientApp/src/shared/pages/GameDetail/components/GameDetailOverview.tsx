import '../styles.css';

import {Button, Col, Descriptions, Image, Rate, Row, Space, Tooltip, Typography} from 'antd';
import React, {useContext} from 'react';
import {Trans, useTranslation} from 'react-i18next';
import {Link, useNavigate} from 'react-router-dom';

import {DeleteOutlined, EditOutlined, PlusOutlined} from '@ant-design/icons';

import {
  GcPageContainer, GcPageContainerContent, GcPageContainerHeader,
} from '../../../components/GcPageContainer';
import {GcStateRibbon} from '../../../components/GcStateRibbon';
import {Game} from '../../../models';
import {createDeleteModal} from '../../../utils';
import {roundToDecimals} from '../../../utils/numberUtils';
import {GamesContext} from '../../Games/context';
import {GameDetailContext} from '../context/GameDetailState';

const { Title } = Typography;

type Props = {
  game: Game;
}

const GameHeader = (props: Props) => {
  const { deleteGame, loading } = useContext(GameDetailContext);
  const { loadGames } = useContext(GamesContext);
  const { game } = props;
  const { t } = useTranslation();
  const navigate = useNavigate();

  const showDeleteModal = () => {
    createDeleteModal(
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

  return (
    <Space direction='vertical' align='start'>
      <Row gutter={8}>
        <Col flex="auto">
          <Title level={2} style={{ margin: 0 }}>
            {game.title}
          </Title>
        </Col>
        <Col>
          <Tooltip placement="right" title={`${game.rating?.toFixed(1)}/10`}>
            <Rate
              disabled
              style={{ fontSize: 10, paddingTop: 10 }}
              allowHalf
              value={roundToDecimals(game.rating, 1) / 2} />
          </Tooltip>
        </Col>
      </Row>
      <Space direction='horizontal' align='start'>
        <Link to={`/plays/${game.id}`}>
          <Button type="primary" icon={<PlusOutlined />} size='small'>{t('game.new-play')}</Button>
        </Link>
        <Button size='small' type='primary' disabled icon={<EditOutlined />}>{t('common.edit')}</Button>
        <Button size='small' type='primary' icon={<DeleteOutlined />} danger onClick={() => showDeleteModal()}>{t('common.delete')}</Button>
      </Space>
      <Descriptions size='small' column={{ xxl: 7, xl: 5, lg: 4, md: 2, sm: 2, xs: 1 }}>
        <Descriptions.Item label="Playtime">{game.minPlayTime} - {game.maxPlayTime} min</Descriptions.Item>
        <Descriptions.Item label="Player count">{game.minPlayers} - {game.maxPlayers}</Descriptions.Item>
        <Descriptions.Item label="Minimum age">{game.minAge}</Descriptions.Item>
        <Descriptions.Item label="Weight">{game.weight} / 5</Descriptions.Item>
      </Descriptions>
      <Descriptions>
        <Descriptions.Item label="Categories">{game.categories.map(x => x.name).join(', ')}</Descriptions.Item>
      </Descriptions>
    </Space>)
}

const GameContent = (props: Props) => {
  const { game } = props;
  const { t } = useTranslation();

  return (
    <>
      {/* <Divider type="horizontal" style={{ margin: '12px 0' }} />
      <Row align="middle">
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px 0 0', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Weight" value={game.weight ?? 0} suffix="/ 5" />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Rating" value={game.rating ?? 0} precision={1} suffix="/ 10" />
        </Col>
        <Col >
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Minimum age" value={game.minAge ?? 0} />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Playtime" value={`${game.minPlayTime} - ${game.maxPlayTime}`} suffix="min" />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 24px', height: '3rem' }} />
        </Col>
        <Col flex="auto">
          <Statistic title="Player count" value={`${game.minPlayers} - ${game.maxPlayers}`} />
        </Col>
        <Col>
          <Divider type="vertical" style={{ margin: '0 0 0 24px', height: '3rem' }} />
        </Col>
      </Row>
      <Divider type="horizontal" style={{ margin: '12px 0' }} />
 */}

    </>
  )
}



const GameDetailOverview = (props: Props) => {
  const { game } = props;
  const { loading } = useContext(GameDetailContext);
  return (
    <GcPageContainer>
      <GcPageContainerHeader
        hasBack
        backNavigation='/games'
        isLoading={loading}
        title={game.title}>
        sdsdf
      </GcPageContainerHeader>
      <GcPageContainerContent isLoading={loading}>
        <Row gutter={[16, 16]}>
          <Col xxl={3} xl={4} md={5} xs={24}>
            <GcStateRibbon state={game.state}>
              <Image
                preview={false}
                width={'100%'}
                src={`https://localhost:7178/${game.image}`}
                className="image"
              />
            </GcStateRibbon>
          </Col>
          <Col xxl={21} xl={20} md={19} xs={24}>
            <GameHeader game={game} />
          </Col>
          <Col xs={24}>
            <GameContent game={game} />
          </Col>
        </Row>
      </GcPageContainerContent>
    </GcPageContainer>
  )
}

export default GameDetailOverview