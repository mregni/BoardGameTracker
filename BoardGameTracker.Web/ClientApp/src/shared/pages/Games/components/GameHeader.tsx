import {Button, Descriptions, Rate, Space, Tooltip} from 'antd';
import {useContext, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {Link} from 'react-router-dom';

import {PlusOutlined} from '@ant-design/icons';

import {roundToDecimals} from '../../../utils';
import {NewPlayDrawer} from '../../Plays';
import {GameDetailContext} from '../context/GameDetailState';

export const GameHeader = () => {
  const { game } = useContext(GameDetailContext);
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  if (game === null) {
    return (<></>);
  }

  return (
    <Space direction='vertical' align='start'>
      <Space direction='horizontal' align='start'>
        <Link to={``} onClick={() => setOpen(true)}>
          <Button type="primary" icon={<PlusOutlined />} size='large'>{t('game.new-play')}</Button>
        </Link>
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
      <Tooltip placement="right" title={`${game.rating?.toFixed(1)}/10`}>
        <Rate
          disabled
          style={{ fontSize: 10, paddingTop: 10 }}
          allowHalf
          value={roundToDecimals(game.rating, 1) / 2} />
      </Tooltip>
      <NewPlayDrawer open={open} close={() => setOpen(false)} game={game} />
    </Space>)
}