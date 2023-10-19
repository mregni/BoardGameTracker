import {Avatar, Space} from 'antd';
import React from 'react';
import {Link} from 'react-router-dom';

type Props = {
  image: string;
  link: string | number;
  label?: string;
}

export const GcAvatar = (props: Props) => {
  const { link, image, label = null } = props;
  console.log(image)
  return (
    <Space>
      <Link to={`/games/${link}`}>
        <Avatar src={`/${image}`} />
      </Link>
      {
        label !== null && (<Link to={`/games/${link}`}>{label}</Link>)
      }
    </Space>
  )
}
