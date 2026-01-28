using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MWModle
{
    public class CRingBuffer
    {
        public struct st_frame_t
        {
            public byte[] p_buffer;
            public Int64 ts;
            public UInt32 buffer_len;
            public UInt32 frame_len;
        }
        public static st_frame_t null_frame;
        protected st_frame_t[] m_frame = null;
        public UInt32 m_buffer_num = 0;
        public UInt32 m_buffer_size = 0;
        protected bool m_writing = false;
        protected Int64 m_write_num = 0;
        protected bool m_rending = false;
        protected Int64 m_render_read_num = -1;
        protected bool m_encoding = false;
        protected Int64 m_encode_read_num = -1;
        public CRingBuffer()
        {
        }
        public virtual void Dispose()
        {
            if (null != m_frame) {
                for (UInt32 i = 0; i < m_buffer_num; i++){
                    m_frame[i].p_buffer = null;
                }
                m_frame = null;
            }
        }
        ~CRingBuffer()
        {
            Dispose();
        }

        public bool set_property(UInt32 buffer_num, UInt32 buffer_size)
        {
            m_frame = new st_frame_t[buffer_num];
            if (null == m_frame){
                return false;
            }
            m_buffer_size = buffer_size;
            for (UInt32 i = 0; i < buffer_num; i++){
                m_frame[i].p_buffer = new byte[buffer_size];
                if (null == m_frame[i].p_buffer){
                    m_buffer_num = i;
                    return false;
                }
                //m_frame[i].ptr = GCHandle.Alloc(m_frame[i].p_buffer, GCHandleType.Pinned).AddrOfPinnedObject();
                m_frame[i].buffer_len = buffer_size;
                m_frame[i].frame_len = 0;
                m_frame[i].ts = 0;
            }
            m_buffer_num = buffer_num;
            return true;
        }

        public st_frame_t get_buffer_to_fill()
        {
            if (m_writing){
                return m_frame[m_write_num % m_buffer_num];
            }
            if (m_buffer_num == 0){
                return null_frame;
            }
            if ((m_render_read_num >= 0) && ((m_write_num - m_render_read_num) >= m_buffer_num)){
                return null_frame;
            }
            if ((m_encode_read_num >= 0) && ((m_write_num - m_encode_read_num) >= m_buffer_num)){
                return null_frame;
            }
            m_writing = true;
            m_frame[m_write_num % m_buffer_num].frame_len = 0;
            return m_frame[m_write_num % m_buffer_num];
        }

        public void buffer_filled(st_frame_t frame)
        {
            if (m_writing){
                m_frame[m_write_num % m_buffer_num] = frame;
                m_write_num++;
                m_writing = false;
            }
        }

        public st_frame_t get_frame_to_render()
        {
            if (m_write_num == 0){
                return null_frame;
            }
            if (m_rending){
                m_render_read_num++;
                m_rending = false;
            }
            else if (m_render_read_num < 0){
                m_render_read_num = ((m_write_num != 0) ? m_write_num : 1) - 1;
            }
            if (m_write_num == m_render_read_num){
                return null_frame;
            }
            if ((m_write_num - m_render_read_num) > (m_buffer_num / 2)){
                m_render_read_num = m_write_num - 1;
            }
            m_rending = true;
            return m_frame[m_render_read_num % m_buffer_num];
        }

        public st_frame_t get_frame_to_encode()
        {
            if (m_write_num == 0){
                return null_frame;
            }
            if (m_encoding){
                m_encode_read_num++;
                m_encoding = false;
            }
            else if (m_encode_read_num < 0){
                m_encode_read_num = ((m_write_num != 0) ? m_write_num : 1) - 1;
            }
            if (m_write_num == m_encode_read_num){
                return null_frame;
            }
            if ((m_write_num - m_encode_read_num) > (m_buffer_num / 2)){
                m_encode_read_num = m_write_num - 1;
            }
            m_encoding = true;
            return m_frame[m_encode_read_num % m_buffer_num];
        }
        public void stop_render()
        {
            m_render_read_num = -1;
        }
        public void stop_encode()
        {
            m_encode_read_num = -1;
        }
        public st_frame_t get_buffer_by_index(int index)
        {
            if (index >= m_buffer_num){
                return null_frame;
            }
            return m_frame[index];
        }
    }
}
