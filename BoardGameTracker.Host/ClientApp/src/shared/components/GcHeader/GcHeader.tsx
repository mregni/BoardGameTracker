import {Layout, Space} from 'antd';

import {useScreenInfo} from '../../hooks/useScreenInfo';
import {GcSmallMenu} from '../GcMenu/GcSmallMenu';

export const GcHeader = () => {
  const { Header } = Layout;
  const { screenMap } = useScreenInfo();

  const height = screenMap.lg ? '64px' : '40px';

  return (
    <Header style={{
      display: 'flex',
      alignItems: 'center',
      paddingLeft: screenMap.lg ? 20 : 10,
      height: height,
      lineHeight: height
    }}
    >
      <Space wrap>
        {!screenMap.lg && (<GcSmallMenu />)}
        Test header
      </Space>
    </Header>
  )
}