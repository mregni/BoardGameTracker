import {Layout, Space} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';

import {GcSmallMenu} from '../GcMenu/GcSmallMenu';

export const GcHeader = () => {
  const { Header } = Layout;
  const screens = useBreakpoint();

  const height = screens.lg ? '64px' : '40px';

  return (
    <Header style={{
      display: 'flex',
      alignItems: 'center',
      paddingLeft: screens.lg ? 20 : 10,
      height: height,
      lineHeight: height
    }}
    >
      <Space wrap>
        {!screens.lg && (<GcSmallMenu />)}
        Test header
      </Space>
    </Header>
  )
}