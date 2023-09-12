import './styles.css';

import {Button, Space} from 'antd';
import React, {Dispatch, SetStateAction} from 'react';
import {Link} from 'react-router-dom';

import {EyeOutlined, PlusOutlined} from '@ant-design/icons';

import {GameState} from '../../models';
import {GcStateRibbon} from '../GcStateRibbon';

interface OverLayProps {
  id: number;
  showPlayLink: boolean;
  detailPage: string;
  setGameId?: Dispatch<SetStateAction<number | null>>
}

const OverLay = (props: OverLayProps) => {
  const { id, showPlayLink, detailPage, setGameId } = props;

  const onClick = () => {
    setGameId && setGameId(id);
  }

  return (
    <div className='overlay'>
      <Space direction='vertical' size={[8, 12]} align="center">
        {
          showPlayLink && <Button type="primary" icon={<PlusOutlined />} size='large' onClick={() => onClick()} />
        }
        <Link to={`/${detailPage}/${id}`}>
          <Button type="primary" icon={<EyeOutlined />} size='large' />
        </Link>
      </Space>
    </div>
  )
}

interface Props {
  title: string;
  id: number;
  image: string;
  showPlayLink?: boolean;
  detailPage: string;
  state?: GameState;
  setGameId?: Dispatch<SetStateAction<number | null>>
}

export const GcCard = (props: Props) => {
  const { title, id, image, state, detailPage, setGameId, showPlayLink = false } = props;
  return (
    <>
      <GcStateRibbon state={state}>
        <div className='card-image'>
          <img
            src={`/${image}`}
            alt={`poster for ${title}`}
            className='image'
          />
          <OverLay
            id={id}
            showPlayLink={showPlayLink}
            detailPage={detailPage}
            setGameId={setGameId}
          />
        </div>
      </GcStateRibbon>
      <div className='title'>{title}</div>
    </>
  )
}